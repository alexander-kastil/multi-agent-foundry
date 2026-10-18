using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

DotNetEnv.Env.TraversePath().Load();

var stages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["stage 1 heiligenblut to elberfelder hut"] = "open, 5 h, 900 m ascent",
    ["stage 2 elberfelder hut to salm hut"] = "open, 4 h, snow above 2600 m",
    ["stage 3 salm hut to glocknerhaus"] = "open, 6 h, exposed ridge",
    ["stage 4 glocknerhaus to kals"] = "closed, rockfall warning",
};

[Description("Return duration, ascent and current condition of one trek stage.")]
string GetStageStatus([Description("Stage name, for example 'Stage 1 Heiligenblut to Elberfelder Hut'.")] string stage) =>
    stages.TryGetValue(stage, out var status) ? status : "unknown stage";

[Description("Reserve beds in a mountain hut. This commits the customer.")]
string ReserveHut([Description("Hut name.")] string hut, [Description("Number of hikers.")] int persons) =>
    $"reserved {persons} bed(s) at {hut}";

var projectClient = new AIProjectClient(
    new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    new DefaultAzureCredential());

IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    Name = "TrekPlanner",
    ChatOptions = new ChatOptions
    {
        Instructions = "You plan multi-day treks for Contoso Outdoor. Check every stage with GetStageStatus before you reserve huts with ReserveHut. Keep every answer under five sentences and avoid dashes.",
        Tools =
        [
            AIFunctionFactory.Create(GetStageStatus, "GetStageStatus"),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ReserveHut, "ReserveHut"))
        ]
    },
    DisableWebSearch = true,
    DisableFileMemory = true,
    MaxContextWindowTokens = 128_000,
    MaxOutputTokens = 16_384,
});

var modeProvider = agent.GetService<AgentModeProvider>()!;

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync(
    "Plan a 4 stage Grossglockner trek for 3 hikers, including a hut reservation for every open stage. The stages are: Stage 1 Heiligenblut to Elberfelder Hut, Stage 2 Elberfelder Hut to Salm Hut, Stage 3 Salm Hut to Glocknerhaus, Stage 4 Glocknerhaus to Kals.",
    session);
Console.WriteLine(response.Messages.Last().Text);
Console.WriteLine($"mode: {await modeProvider.GetModeAsync(session)}");


response = await agent.RunAsync("The plan is approved. Switch to execute mode and execute it, including the hut reservations.", session);
var requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
while (requests.Count > 0)
{
    var replies = requests.ConvertAll(request =>
    {
        var call = (FunctionCallContent)request.ToolCall;
        Console.WriteLine($"approval requested: {call.Name}({string.Join(", ", call.Arguments!.Select(a => $"{a.Key}={a.Value}"))})");
        return new ChatMessage(ChatRole.User, [request.CreateResponse(true)]);
    });

    response = await agent.RunAsync(replies, session);
    requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
}

Console.WriteLine(response.Messages.Last().Text);
