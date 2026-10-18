using System.Globalization;

DotNetEnv.Env.TraversePath().Load();
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

double Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

Dictionary<string, string> versions = await Foundry.DeployPricingVersions();
Dictionary<string, string> deployedPrompts = [];
foreach ((string label, string version) in versions)
    deployedPrompts[label] = await Foundry.ReadInstructions(version);

SnapshotStore store = new();
List<Request> requests = [];
List<Change> changeLog = [];
string[] orderIds = Pipeline.Orders.Keys.ToArray();

Console.WriteLine("== Production traffic ==");
foreach (string label in new[] { "v1", "v2" })
{
    if (label == "v2") changeLog.Add(new Change(Now(), "prompt_deployment", "pricing-agent", "v1", "v2"));
    for (int index = 0; index < 6; index++)
    {
        TraceRecorder recorder = new();
        string orderId = orderIds[index % orderIds.Length];
        (string priceText, Payment payment) = await Pipeline.RunCheckout(orderId, versions[label], recorder);
        bool success = payment.Status == "paid";
        requests.Add(new Request(
            Now(),
            recorder.TraceId,
            success,
            recorder.Steps.First(step => step.Name == "agent_pricing").DurationMs,
            recorder.Steps.First(step => step.Name == "tool_order_lookup").DurationMs,
            priceText));
        if (!success) store.Save(Pipeline.BuildSnapshot(recorder, orderId, label));
    }
}
List<Request> failed = requests.Where(request => !request.Success).ToList();
double errorRate = (double)failed.Count / requests.Count;
Console.WriteLine($"{requests.Count} checkouts, {failed.Count} failed, error rate {errorRate * 100:F0}%, severity {Severity.Classify(errorRate)}");

Console.WriteLine("\n== Root cause analysis ==");
foreach ((string name, string stats) in Rca.Differential(requests))
    Console.WriteLine($"{name}: {stats}");
foreach (Hypothesis result in new HypothesisFramework(changeLog, requests).Investigate())
    Console.WriteLine($"hypothesis '{result.Name}': {(result.Confirmed ? "CONFIRMED" : "eliminated")}");
double firstFailure = Rca.FirstFailureAfter(requests, changeLog[0].Time)!.Value;
Console.WriteLine($"first failure {firstFailure - changeLog[0].Time:F1}s after the prompt deployment");

Console.WriteLine("\n== Replay ==");
Snapshot snapshot = store.Load(failed[0].TraceId);
Console.WriteLine($"snapshot {snapshot.TraceId[..8]} production output: '{snapshot.ProductionOutput}'");
Console.WriteLine($"hash check against current v1 prompt: [{string.Join(", ", AgentReplayFramework.VerifyPromptHashes(snapshot, new() { ["pricing-agent"] = deployedPrompts["v1"] }))}]");
ReplayResult reproduced = await AgentReplayFramework.ReplayPricingStep(snapshot);
Console.WriteLine($"replay with captured prompt: output '{reproduced.Output}' valid {reproduced.Valid}");
(Dictionary<string, bool> results, string? firstFailing) = await AgentReplayFramework.BisectPromptVersions(snapshot, deployedPrompts);
Console.WriteLine($"bisection {{{string.Join(", ", results.Select(pair => $"{pair.Key}: {pair.Value}"))}}} first failing version {firstFailing}");
await Foundry.DeletePricingVersions(versions);

Console.WriteLine("\n== Automated remediation ==");
Gateway primary = new("primary", healthy: false);
Gateway backup = new("backup");
Dictionary<string, string> config = new() { ["payment_gateway"] = "primary" };
CircuitBreaker breaker = new(window: 20, probeIntervalSeconds: 0.5);
List<string> servedBy = [];
for (int i = 0; i < 14; i++) servedBy.Add(breaker.Call(() => primary.Charge(10), () => backup.Charge(10)).Gateway);
Console.WriteLine($"breaker state {breaker.State}, last charges served by [{string.Join(", ", servedBy.TakeLast(3))}]");
primary.Healthy = true;
Thread.Sleep(600);
Console.WriteLine($"after probe: {breaker.Call(() => primary.Charge(10), () => backup.Charge(10)).Gateway}, state {breaker.State}");
primary.Healthy = false;
GatewayRunbook runbook = new(primary, backup, config);
Console.WriteLine($"runbook run 1: {runbook.Run()}");
Console.WriteLine($"runbook run 2: {runbook.Run()}");
Console.WriteLine($"gateway config: {{payment_gateway: {config["payment_gateway"]}}}");

Incident incident = new(
    "Checkout failures after pricing agent prompt update",
    $"After pricing-agent v2 was deployed, {failed.Count} of {requests.Count} checkouts failed ({errorRate * 100:F0}%) because the payment step rejected prices containing a currency symbol.",
    [
        "Pricing agent prompt v2 deployed to 100% of traffic",
        $"First failed checkout {firstFailure - changeLog[0].Time:F1}s later",
        "Snapshots captured automatically for every failed trace",
        "Hypothesis framework confirmed prompt regression after eliminating model deployment change",
        "Replay with the v1 prompt produced valid output, bisection isolated v2",
    ],
    "Prompt v2 instructs the pricing agent to include the currency symbol and an explanation, which breaks the numeric contract with the payment step.",
    [
        "No output format validation between pricing and payment agents",
        "No canary deployment for prompt changes",
        "Regression suite had no price format probe",
    ],
    ["Failed traces were captured for replay without manual flagging", "Bisection isolated the faulty prompt version in one replay run"],
    ["No automated alert fired on the checkout error rate until review"],
    [
        new("Add output format validation to the pricing agent", "ai-engineering", "2026-10-25"),
        new("Canary every prompt deployment at 5% traffic", "platform", "2026-11-01"),
        new("Add price format probe to the regression suite", "ai-engineering", "2026-10-25"),
    ]);
Postmortem.Write("postmortem-output.md", incident);
Console.WriteLine("\nPostmortem written to postmortem-output.md");
