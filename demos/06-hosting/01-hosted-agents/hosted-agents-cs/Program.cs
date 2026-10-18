using Azure.AI.AgentServer.Core;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;

var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var deployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME")
    ?? Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

Dictionary<string, string> tickets = new()
{
    ["T-1001"] = "Printer on floor 3 offline, assigned to facilities, priority low",
    ["T-1002"] = "VPN drops every hour for the sales team, assigned to network, priority high",
    ["T-1003"] = "New starter laptop request, assigned to procurement, priority medium",
};

AIFunction lookupTicket = AIFunctionFactory.Create(
    (string ticketId) => tickets.GetValueOrDefault(ticketId.ToUpperInvariant(), $"No ticket {ticketId} found"),
    "lookup_ticket",
    "Return the status line for a support ticket id such as T-1001.");

AIAgent agent = new AIProjectClient(projectEndpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You are the Contoso support desk agent. Use lookup_ticket to answer questions about tickets and keep answers to two sentences.",
        name: "m6-support-desk-cs",
        tools: [lookupTicket]);

var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
