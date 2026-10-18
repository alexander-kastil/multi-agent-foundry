using System.ComponentModel;
using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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

var modelCalls = 0;

async Task<ChatResponse> ModelMeter(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    CancellationToken cancellationToken)
{
    modelCalls++;
    Console.WriteLine($"[chat] call {modelCalls}: {messages.Count()} message(s) to the model");
    var response = await innerChatClient.GetResponseAsync(messages, options, cancellationToken);
    Console.WriteLine($"[chat] call {modelCalls}: {response.Usage?.TotalTokenCount} tokens");
    return response;
}

AIAgent baseAgent = new AIProjectClient(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential())
    .AsAIAgent(
        model: Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!,
        instructions: "You are a trail guide for Contoso Outdoor. Use GetTrailStatus before you recommend a trail. Answer in two sentences without dashes.",
        name: "TrailGuide",
        tools: [AIFunctionFactory.Create(GetTrailStatus, "GetTrailStatus")],
        clientFactory: chatClient => chatClient
            .AsBuilder()
            .Use(getResponseFunc: ModelMeter, getStreamingResponseFunc: null)
            .Build());

async Task<AgentResponse> Guardrail(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    var text = messages.Last().Text.ToLowerInvariant();
    if (new[] { "password", "secret", "api key" }.Any(text.Contains))
    {
        Console.WriteLine("[guardrail] blocked");
        return new AgentResponse(new ChatMessage(ChatRole.Assistant, "I can't help with credentials or secrets."));
    }

    Console.WriteLine("[guardrail] passed");
    var stopwatch = Stopwatch.StartNew();
    var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
    Console.WriteLine($"[agent] done in {stopwatch.ElapsedMilliseconds} ms");
    return response;
}

async ValueTask<object?> ToolAudit(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"[tool] {context.Function.Name}({string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}"))})");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"[tool] result: {result}");
    return result;
}

AIAgent agent = baseAgent
    .AsBuilder()
    .Use(runFunc: Guardrail, runStreamingFunc: null)
    .Use(ToolAudit)
    .Build();

foreach (var prompt in new[] { "Can I hike the Dachstein traverse tomorrow?", "What is the admin password of the booking system?" })
{
    Console.WriteLine($"user: {prompt}");
    Console.WriteLine($"agent: {await agent.RunAsync(prompt)}\n");
}
