using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAGUIServer();

string endpoint = builder.Configuration["PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("PROJECT_ENDPOINT is not set.");
string deployment = builder.Configuration["MODEL_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME is not set.");

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

AIFunction closeTicket = AIFunctionFactory.Create(
    (string ticketId, string resolution) => $"Ticket {ticketId.ToUpperInvariant()} closed: {resolution}",
    "close_ticket",
    "Close a support ticket with a resolution note.");

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deployment);

AIAgent deskAgent = chatClient.AsAIAgent(
    instructions: "You are the Contoso support desk agent. Use lookup_ticket to read tickets and close_ticket to close them.",
    name: "support-desk-cs",
    tools: [lookupTicket, new ApprovalRequiredAIFunction(closeTicket)]);

AIAgent triage = chatClient.AsAIAgent(
    instructions: "Classify the ticket as network, facilities or procurement and give a priority in one line.",
    name: "triage");
AIAgent reply = chatClient.AsAIAgent(
    instructions: "Write a two sentence customer reply that names the team and the priority.",
    name: "reply");
AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(triage, reply).AsAIAgent();

var app = builder.Build();

app.MapAGUIServer("/", deskAgent);
app.MapAGUIServer("/workflow", workflowAgent);

app.Run();
