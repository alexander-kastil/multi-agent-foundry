using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

DotNetEnv.Env.TraversePath().Load();

var trails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["tauern high route"] = "open, snow above 2400 m",
    ["wachau valley loop"] = "open, dry",
    ["dachstein traverse"] = "closed, rockfall warning",
};

[Description("Return the current status of a hiking trail.")]
string GetTrailStatus([Description("Trail name, for example 'Wachau valley loop'.")] string trail) =>
    trails.TryGetValue(trail, out var status) ? status : "unknown trail";

[Description("Estimate the hut price in euro for a stay: 38 euro per person and night.")]
string EstimatePrice([Description("Number of nights.")] int nights, [Description("Number of hikers.")] int persons) =>
    $"{nights * persons * 38} euro";

[Description("Book a mountain hut. This charges the customer and cannot be undone.")]
string BookHut(
    [Description("Hut name, for example 'Erzherzog Johann Hut'.")] string hut,
    [Description("Number of nights.")] int nights,
    [Description("Number of hikers.")] int persons) =>
    $"booked {hut} for {persons} person(s), {nights} night(s), confirmation HB-{hut.Sum(c => c) % 9000 + 1000}";

var approve = args.Length > 0 && args[0] == "approve";

var projectClient = new AIProjectClient(
    new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    new DefaultAzureCredential());
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
const string instructions = "You are a trail guide for Contoso Outdoor. Answer in two sentences without dashes.";

AIAgent hutAgent = projectClient.AsAIAgent(
    model: model,
    instructions: instructions,
    name: "TrailGuide",
    tools:
    [
        AIFunctionFactory.Create(GetTrailStatus, "GetTrailStatus"),
        AIFunctionFactory.Create(EstimatePrice, "EstimatePrice"),
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(BookHut, "BookHut"))
    ]);

AgentSession session = await hutAgent.CreateSessionAsync();
AgentResponse response = await hutAgent.RunAsync("Is the Wachau valley loop open, and book the Erzherzog Johann Hut for 2 hikers for 1 night.", session);

var requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
while (requests.Count > 0)
{
    var replies = new List<AIContent>();
    foreach (var request in requests)
    {
        var call = (FunctionCallContent)request.ToolCall;
        Console.WriteLine($"approval requested: {call.Name}({string.Join(", ", call.Arguments!.Select(a => $"{a.Key}={a.Value}"))})");
        Console.WriteLine($"decision: {(approve ? "approve" : "reject")}");
        replies.Add(request.CreateResponse(approve));
    }

    response = await hutAgent.RunAsync(new ChatMessage(ChatRole.User, replies), session);
    requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
}

Console.WriteLine(response);

await using McpClient learn = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
    Name = "microsoft-learn"
}));

IList<McpClientTool> mcpTools = await learn.ListToolsAsync();
Console.WriteLine($"mcp tools: {string.Join(", ", mcpTools.Select(t => t.Name))}");

AIAgent docsAgent = projectClient.AsAIAgent(
    model: model,
    instructions: "Search Microsoft Learn before you answer. Answer in two sentences without dashes.",
    name: "DocsGuide",
    tools: [.. mcpTools]);

Console.WriteLine(await docsAgent.RunAsync("Which package contains the C# AsAIAgent extension for Microsoft Foundry?"));
