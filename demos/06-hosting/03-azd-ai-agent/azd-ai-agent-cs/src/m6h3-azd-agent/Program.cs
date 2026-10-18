using Azure.AI.AgentServer.Core;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;

var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!);
var deployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME")!;

Dictionary<string, string> releases = new()
{
    ["portal"] = "2.4.0, shipped 2026-10-01, adds SSO and a new audit log",
    ["gateway"] = "1.9.2, shipped 2026-09-24, fixes a token refresh bug",
    ["mobile"] = "3.1.0, shipped 2026-10-12, adds offline mode",
};

AIFunction latestRelease = AIFunctionFactory.Create(
    (string product) => releases.GetValueOrDefault(product.ToLowerInvariant(), $"No release data for {product}"),
    "latest_release",
    "Return the latest release line for portal, gateway or mobile.");

AIAgent agent = new AIProjectClient(projectEndpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You are the Contoso release notes agent. Use latest_release and answer in one sentence.",
        name: "m6h3-azd-agent",
        tools: [latestRelease]);

var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
