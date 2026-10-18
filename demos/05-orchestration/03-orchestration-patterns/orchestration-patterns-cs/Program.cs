using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

DotNetEnv.Env.TraversePath().Load();

var endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!;
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
var projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

AIAgent Agent(string name, string instructions, string description = "") =>
    projectClient.AsAIAgent(model: model, instructions: instructions, name: name, description: description);

async Task<List<ChatMessage>> RunAsync(Workflow workflow, string prompt)
{
    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new List<ChatMessage> { new(ChatRole.User, prompt) });
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        if (evt is WorkflowOutputEvent output and not AgentResponseUpdateEvent)
        {
            return output.As<List<ChatMessage>>()!;
        }
    }
    return [];
}

Console.WriteLine("Sequential");
var writer = Agent("writer", "You write a three sentence product announcement from the brief.");
var reviewer = Agent("reviewer", "You review the announcement above and list at most two concrete improvements.");
var polisher = Agent("polisher", "You apply the review to the announcement and reply with the final text only.");
var sequential = AgentWorkflowBuilder.BuildSequential([writer, reviewer, polisher]);
foreach (var message in await RunAsync(sequential, "Announce that our expense app now supports receipt scanning from the phone camera."))
{
    if (!string.IsNullOrWhiteSpace(message.Text)) Console.WriteLine($"[{message.AuthorName ?? message.Role.Value}] {message.Text}");
}

Console.WriteLine("\nConcurrent");
var finance = Agent("finance", "You assess the cost and savings of the proposal in two sentences.");
var security = Agent("security", "You assess the security and fraud risks of the proposal in two sentences.");
var compliance = Agent("compliance", "You assess tax and data protection compliance of the proposal in two sentences.");
var concurrent = AgentWorkflowBuilder.BuildConcurrent([finance, security, compliance]);
foreach (var message in await RunAsync(concurrent, "We want to let employees scan receipts with their phone and auto-approve expenses under 50 EUR."))
{
    if (!string.IsNullOrWhiteSpace(message.Text)) Console.WriteLine($"[{message.AuthorName ?? message.Role.Value}] {message.Text}");
}

Console.WriteLine("\nHandoff");
var triage = Agent("triage_agent", "You are frontline support. Always hand off to the billing or technical specialist.", "Routes customers to specialists");
var billing = Agent("billing_agent", "You resolve billing questions in two sentences.", "Handles invoices and refunds");
var technical = Agent("technical_agent", "You resolve technical questions in two sentences.", "Handles app errors and crashes");
var handoff = AgentWorkflowBuilder.CreateHandoffBuilderWith(triage)
    .WithHandoffs(triage, [billing, technical])
    .WithHandoffs([billing, technical], triage)
    .Build();
foreach (var message in await RunAsync(handoff, "I was charged twice for my subscription this month."))
{
    if (!string.IsNullOrWhiteSpace(message.Text)) Console.WriteLine($"[{message.AuthorName ?? message.Role.Value}] {message.Text}");
}
