using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

DotNetEnv.Env.TraversePath().Load();

var altitude = new AgentInlineSkill(
        name: "altitude",
        description: "Convert mountain altitudes between meters and feet. Use when a customer gives a summit or hut height in one unit and needs the other.",
        instructions: "Use the convert script. Pass the value and the factor (3.28084 for meters to feet, 0.3048 for feet to meters).")
    .AddScript("convert", (double value, double factor) =>
        JsonSerializer.Serialize(new { value, factor, result = Math.Round(value * factor, 1) }));

var skillsProvider = new AgentSkillsProviderBuilder()
    .UseFileSkill(Path.Combine(AppContext.BaseDirectory, "skills"))
    .UseSkill(altitude)
    .UseFileScriptRunner(ScriptRunner.RunAsync)
    .Build();

AIAgent agent = new AIProjectClient(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential())
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "HutDesk",
        ChatOptions = new()
        {
            ModelId = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME"),
            Instructions = "You are the hut desk of Contoso Outdoor. Use skills when they match the request."
        },
        AIContextProviders = [skillsProvider]
    });

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync(
    "Quote 3 nights for 4 hikers in winter and tell me how many feet 3019 meters are.", session);

var requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
while (requests.Count > 0)
{
    var replies = requests.ConvertAll(request =>
    {
        var call = (FunctionCallContent)request.ToolCall;
        Console.WriteLine($"approved: {call.Name}({string.Join(", ", call.Arguments!.Select(a => $"{a.Key}={a.Value}"))})");
        return new ChatMessage(ChatRole.User, [request.CreateResponse(true)]);
    });

    response = await agent.RunAsync(replies, session);
    requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
}

Console.WriteLine(response);
