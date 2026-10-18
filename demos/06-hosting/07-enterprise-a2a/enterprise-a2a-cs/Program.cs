using A2A;
using A2A.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;
using EnterpriseA2A;
using Microsoft.Extensions.AI;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

AsyncLocal<string?> tenantContext = new();

AgentCard CreateCard(string name, string url, params string[] skills) => new()
{
    Name = name,
    Description = name,
    Version = "2.3.1",
    Skills = skills.Select(skill => new AgentSkill { Id = skill, Name = skill, Description = skill, Tags = [skill] }).ToList(),
    SupportedInterfaces = [new AgentInterface { Url = url, ProtocolBinding = ProtocolBindingNames.HttpJson, ProtocolVersion = "1.0" }],
};

async Task<WebApplication> StartCardHostAsync(AgentCard card, int port)
{
    WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
    WebApplication host = builder.Build();
    host.MapWellKnownAgentCard(card);
    await host.StartAsync();
    return host;
}

async Task<AgentRegistry> DemoRegistryAsync()
{
    Console.WriteLine("Discovery registry and capability routing");
    (AgentCard Card, int Port)[] fleet =
    [
        (CreateCard("market-analysis-v2-eastus", "https://market-eus.contoso.com", "sector-analysis", "trend-forecasting"), 18901),
        (CreateCard("market-analysis-v2-westeurope", "https://market-weu.contoso.com", "sector-analysis"), 18902),
        (CreateCard("risk-v1-eastus", "https://risk-eus.contoso.com", "portfolio-risk"), 18903),
    ];
    List<WebApplication> hosts = [];
    AgentRegistry registry = new(TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(120));
    foreach ((AgentCard card, int port) in fleet)
    {
        hosts.Add(await StartCardHostAsync(card, port));
        AgentCard discovered = await new A2ACardResolver(new Uri($"http://127.0.0.1:{port}")).GetAgentCardAsync();
        registry.Register(discovered);
    }
    Console.WriteLine($"cards discovered over /.well-known/agent-card.json: {registry.Items.Count}");
    Console.WriteLine($"sector-analysis candidates: [{string.Join(", ", registry.FindByCapability("sector-analysis").Select(a => a.AgentId))}]");
    Console.WriteLine($"round robin: [{string.Join(", ", Enumerable.Range(0, 4).Select(_ => registry.Route("sector-analysis").AgentId))}]");
    registry.Heartbeat("market-analysis-v2-westeurope", "draining");
    Console.WriteLine($"after draining westeurope: [{string.Join(", ", registry.FindByCapability("sector-analysis").Select(a => a.AgentId))}]");
    registry.Items["market-analysis-v2-eastus"].LastHeartbeat -= TimeSpan.FromSeconds(600);
    Console.WriteLine($"after eastus heartbeat goes stale: [{string.Join(", ", registry.FindByCapability("sector-analysis").Select(a => a.AgentId))}]");
    foreach (WebApplication host in hosts) await host.StopAsync();
    return registry;
}

async Task<SharedStateStore> DemoSharedStateAsync()
{
    Console.WriteLine("\nShared state with ETag concurrency and cache invalidation");
    SharedStateStore store = new();
    int invalidations = 0;
    store.Subscribe(_ => Interlocked.Increment(ref invalidations));
    store.Create("acme-retirement", "tech-analysis");
    store.Read("acme-retirement", "tech-analysis");
    store.Read("acme-retirement", "tech-analysis");
    Console.WriteLine($"two cached reads cost {store.StoreReads} store read(s)");
    string[] agents = ["market-analysis-v2", "risk-v1", "compliance-v1", "reporting-v1"];
    int[] attempts = await Task.WhenAll(agents.Select(agent =>
        Task.Run(() => store.ContributeAsync("acme-retirement", "tech-analysis", agent, new JsonObject { ["note"] = $"from {agent}" }))));
    JsonObject document = store.Read("acme-retirement", "tech-analysis", useCache: false);
    JsonObject stale = store.Read("acme-retirement", "tech-analysis", useCache: false);
    await store.ContributeAsync("acme-retirement", "tech-analysis", "esg-v1", new JsonObject { ["note"] = "newer write" });
    ((JsonObject)stale["contributions"]!)["late-agent"] = new JsonObject { ["version"] = 99 };
    bool accepted = store.ReplaceIfUnmodified("acme-retirement", "tech-analysis", stale, (string)stale["_etag"]!);
    Console.WriteLine($"write with a stale etag accepted: {accepted}");
    Console.WriteLine($"attempts per agent: [{string.Join(", ", attempts)}], contributions kept: [{string.Join(", ", ((JsonObject)document["contributions"]!).Select(c => c.Key).Order())}]");
    Console.WriteLine($"invalidation messages published: {invalidations}");
    return store;
}

void DemoIsolation(SharedStateStore store)
{
    Console.WriteLine("\nTenant isolation");
    store.Create("other-client", "tech-analysis");
    store.ContributeAsync("other-client", "tech-analysis", "market-analysis-v2", new JsonObject { ["note"] = "other client view" }).GetAwaiter().GetResult();
    tenantContext.Value = "acme-retirement";
    JsonObject own = store.Read(tenantContext.Value, "tech-analysis");
    Console.WriteLine($"acme sees agents: [{string.Join(", ", ((JsonObject)own["contributions"]!).Select(c => c.Key).Order())}]");
    Console.WriteLine($"acme task list: [{string.Join(", ", store.QueryTenantTasks("acme-retirement"))}]");
    Console.WriteLine($"other-client task list: [{string.Join(", ", store.QueryTenantTasks("other-client"))}]");
    Console.WriteLine($"cache keys carry the tenant: [{string.Join(", ", store.CacheKeys.Order())}]");
}

async Task DemoConflictsAsync(IChatClient chat)
{
    Console.WriteLine("\nConflict detection, resolution and audit trail");
    AuditTrail audit = new();

    JsonObject market = new() { ["analysis"] = "MSFT shows strong growth indicators and a favorable setup.", ["recommendation"] = new JsonObject { ["symbol"] = "MSFT", ["action"] = "increase" } };
    JsonObject risk = new() { ["analysis"] = "MSFT faces elevated sector concentration risk.", ["recommendation"] = new JsonObject { ["symbol"] = "MSFT", ["action"] = "decrease" } };
    ConflictReport report = await Conflicts.DetectAsync(chat, "market-analysis-v2-eastus", market, "risk-v1-eastus", risk);
    Resolution resolution = Conflicts.Resolve(report);
    audit.Log("acme-retirement", report, resolution);
    Console.WriteLine($"rule-based: conflict={report.Conflict} type={report.Type} resolution={resolution.Method} chosen={resolution.ChosenAgent}");

    JsonObject bull = new() { ["analysis"] = "Growth prospects are strong and valuation is justified, so we should accumulate shares.", ["recommendation"] = new JsonObject { ["symbol"] = "MSFT" } };
    JsonObject bear = new() { ["analysis"] = "Valuation is stretched and earnings risk is high, so we should reduce the holding.", ["recommendation"] = new JsonObject { ["symbol"] = "MSFT" } };
    report = await Conflicts.DetectAsync(chat, "market-analysis-v2-eastus", bull, "market-analysis-v2-westeurope", bear);
    resolution = Conflicts.Resolve(report);
    audit.Log("acme-retirement", report, resolution);
    Console.WriteLine($"semantic: conflict={report.Conflict} type={report.Type} confidence={report.Confidence} resolution={resolution.Outcome}");

    Console.WriteLine($"audit entries: {audit.Entries.Count}");
    Console.WriteLine(JsonSerializer.Serialize(audit.Entries[0], new JsonSerializerOptions { WriteIndented = true }));
}

string endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("PROJECT_ENDPOINT is not set.");
string deployment = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")
    ?? throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME is not set.");

await DemoRegistryAsync();
SharedStateStore store = await DemoSharedStateAsync();
DemoIsolation(store);

IChatClient chat = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deployment);
await DemoConflictsAsync(chat);
