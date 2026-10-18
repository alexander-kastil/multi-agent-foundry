using System.Globalization;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

const string AgentId = "support-agent";

string[] requests =
[
    "What is the status of order #12345?",
    "Where is order 12345 right now?",
    "Which gift box suits a vegan friend best: the truffle box, the praline box or the dark chocolate set?",
    "I want an exception to your return policy for damaged goods from three months ago, otherwise I will file a legal complaint.",
    "What is the status of order #12345?",
    "what's the status of my order 12345",
];

List<Turn> chainHistory =
[
    new("user", "Hi, I am a gold member and I prefer email communication. I need to return something."),
    new("assistant", "Happy to help. Which order? I'll follow up by email once I have the details."),
    new("user", "Order #12345, the truffle box."),
    new("assistant", "Order 12345 was delivered 42 days ago. A return is not eligible under the 30 day policy."),
    new("user", "It arrived damaged though."),
    new("assistant", "I understand. Let me check the exception options for damaged goods."),
    new("user", "Please make sure to note it is for my business account."),
    new("assistant", "Noted for your business account. Checking now."),
    new("user", "How long will this take?"),
    new("assistant", "A reviewer will answer within four hours."),
    new("user", "Ok and what is the refund amount?"),
];

Dictionary<string, object> productRecord = new() { ["product_name"] = "Truffle Box", ["current_price"] = 79.99, ["stock_level"] = 40 };
for (int i = 0; i < 48; i++) productRecord[$"field_{i}"] = new string('x', 60);
Dictionary<string, object> orderRecord = new() { ["order_id"] = "12345", ["fulfillment_status"] = "delivered", ["items"] = new List<string> { "A" } };
for (int i = 0; i < 30; i++) orderRecord[$"audit_{i}"] = new string('y', 60);

string Short(string text) => text[..Math.Min(60, text.Length)];

async Task Routing()
{
    Console.WriteLine("== Routing with multi-level cache ==");
    ModelRouter router = new();
    HashStore store = new();
    SemanticCache semanticCache = new(store, Foundry.Embeddings());
    ResultCache resultCache = new(store);

    double baselineCost = 0;
    double baselineLatency = 0;
    foreach (string question in requests)
    {
        RouteOutcome outcome = await router.RouteWithQualityFloor(question, maxTier: 2, forceTier: 2);
        baselineCost += outcome.Cost;
        baselineLatency += outcome.LatencyS;
    }
    Console.WriteLine($"baseline, every request on tier 2 without caches: ${baselineCost:F5} in {baselineLatency:F1}s");

    double totalCost = 0;
    double totalLatency = 0;
    foreach (string question in requests)
    {
        Dictionary<string, string> signature = new() { ["q"] = question };
        if (resultCache.Get(AgentId, signature) is not null)
        {
            Console.WriteLine($"result-cache hit | {Short(question)}");
            continue;
        }
        (bool hit, _, double similarity) = await semanticCache.CheckCache(question, AgentId);
        if (hit)
        {
            Console.WriteLine($"semantic hit {similarity:F2} | {Short(question)}");
            continue;
        }
        RouteOutcome outcome = await router.RouteWithQualityFloor(question);
        totalCost += outcome.Cost;
        totalLatency += outcome.LatencyS;
        await semanticCache.StoreCache(question, outcome.Response, AgentId, ["order-database"]);
        resultCache.Put(AgentId, signature, outcome.Response, ["order-database"]);
        Console.WriteLine($"model call tier {outcome.AssignedTier}->{outcome.TierUsed} quality {outcome.QualityScore} cost ${outcome.Cost:F5} {outcome.LatencyS:F1}s | {Short(question)}");
    }
    Console.WriteLine($"Result cache hits {resultCache.Hits}, semantic cache hits {semanticCache.Hits}, model calls {requests.Length - resultCache.Hits - semanticCache.Hits}");
    Console.WriteLine($"routed with caches: ${totalCost:F5} in {totalLatency:F1}s");
    Console.WriteLine($"Invalidated {semanticCache.InvalidateByDependency("order-database")} entries after order-database change");
}

async Task PrefixCaching()
{
    Console.WriteLine("== Prompt prefix caching ==");
    string stablePrefix = "Return policy handbook. " + string.Join(" ", Enumerable.Range(0, 120).Select(i => $"Rule {i}: refunds are issued to the original payment method within five business days."));
    foreach (int attempt in new[] { 1, 2 })
    {
        ResponseResult response = await Foundry.Respond(Foundry.Model("TIER2_MODEL"), $"Question {attempt}: how long do refunds take? Answer in one sentence.", stablePrefix);
        Console.WriteLine($"call {attempt}: input tokens {response.Usage.InputTokenCount}, cached {response.Usage.InputTokenDetails.CachedTokenCount}");
    }
}

void ContextBudget()
{
    Console.WriteLine("== Token budgets and context assembly ==");
    ContextAssembler assembler = new();
    string systemPrompt = "You are the return eligibility agent. Apply the return policy.";
    Dictionary<string, Dictionary<string, object>> toolResults = new() { ["product_lookup"] = productRecord, ["order_lookup"] = orderRecord };
    string raw = systemPrompt + string.Join(" ", chainHistory.Select(turn => turn.Content)) + string.Join(" ", toolResults.SelectMany(pair => pair.Value.Select(field => $"{field.Key}={field.Value}")));
    int naive = assembler.CountTokens(raw);
    foreach ((string agentType, int budget) in new[] { ("lookup", 120), ("reasoning", 4000) })
    {
        (int used, List<string> pruned) = assembler.AssembleContext(chainHistory, systemPrompt, toolResults, budget);
        Console.WriteLine($"{agentType}: naive {naive} tokens -> assembled {used} tokens (budget {budget}) pruned [{string.Join(", ", pruned.Select(p => $"'{p}'"))}]");
    }
}

void TradeoffsSection()
{
    Console.WriteLine("== Quality, cost and latency trade-offs ==");
    foreach ((string segment, SegmentProfile profile) in Tradeoffs.SegmentProfiles)
    {
        (bool latencyOk, bool costOk) = Tradeoffs.MeetsSlo(segment, 2.5, 0.004);
        Console.WriteLine($"{segment}: tiers [{string.Join(", ", profile.DefaultTiers)}] history {profile.HistoryTurns} turns cache {profile.CacheThreshold} SLO latency_ok={latencyOk} cost_ok={costOk}");
    }
    QualityCircuitBreaker breaker = new(window: 10);
    foreach (double rating in new[] { 4.5, 4.0, 3.2, 3.0, 3.5, 3.1 }) breaker.Record(rating);
    Console.WriteLine($"Circuit breaker open: {breaker.Open} settings {breaker.Settings("{model_tier: 1, cache_enabled: True, history_turns: 3}")}");
}

async Task ParallelCalls()
{
    Console.WriteLine("== Parallel agent calls ==");
    string[] prompts =
    [
        "Name one dark chocolate flavor note.",
        "Name one milk chocolate flavor note.",
        "Name one white chocolate flavor note.",
    ];
    double sequential = await Tradeoffs.TimedCalls(prompts, parallel: false);
    double parallel = await Tradeoffs.TimedCalls(prompts, parallel: true);
    Console.WriteLine($"3 independent agent calls: sequential {sequential:F1}s, parallel {parallel:F1}s");
}

Dictionary<string, Func<Task>> sections = new()
{
    ["routing"] = Routing,
    ["prefix"] = PrefixCaching,
    ["context"] = () => { ContextBudget(); return Task.CompletedTask; },
    ["tradeoffs"] = () => { TradeoffsSection(); return Task.CompletedTask; },
    ["parallel"] = ParallelCalls,
};

string[] selected = args.Length > 0 ? args : sections.Keys.ToArray();
for (int i = 0; i < selected.Length; i++)
{
    if (i > 0) Console.WriteLine();
    await sections[selected[i]]();
}
