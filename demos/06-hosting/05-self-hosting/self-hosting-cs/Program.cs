using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

string endpoint = builder.Configuration["PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("PROJECT_ENDPOINT is not set.");
string deployment = builder.Configuration["MODEL_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME is not set.");

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deployment);
builder.Services.AddSingleton(chatClient);

builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

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

var supportDesk = builder.AddAIAgent(
    "support-desk",
    (services, name) => services.GetRequiredService<IChatClient>().AsAIAgent(
        instructions: "You are the Contoso support desk agent. Use lookup_ticket for ticket questions and answer in two sentences.",
        name: name,
        tools: [lookupTicket]));

var app = builder.Build();

app.MapOpenAIResponses(supportDesk);
app.MapOpenAIConversations();

app.Run();
