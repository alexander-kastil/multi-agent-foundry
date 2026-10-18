using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

DotNetEnv.Env.TraversePath().Load();

var projectClient = new AIProjectClient(
    new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    new DefaultAzureCredential());

ChatClientAgent agent = projectClient.AsAIAgent(
    model: Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!,
    instructions: "You are a trip planner for Contoso Outdoor. Answer in two sentences and use what the user told you earlier.",
    name: "TripPlanner");

var sessionFile = Path.Combine("sessions", "session.json");

AgentSession session;
if (File.Exists(sessionFile))
{
    JsonElement saved = JsonDocument.Parse(await File.ReadAllTextAsync(sessionFile)).RootElement;
    session = await agent.DeserializeSessionAsync(saved);
    Console.WriteLine("resumed session");
}
else
{
    session = await agent.CreateSessionAsync();
    Console.WriteLine("new session");
}

Console.WriteLine(await agent.RunAsync(args[0], session));

Directory.CreateDirectory("sessions");
JsonElement serialized = await agent.SerializeSessionAsync(session);
await File.WriteAllTextAsync(sessionFile, JsonSerializer.Serialize(serialized, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(await File.ReadAllTextAsync(sessionFile));

var conversationId = serialized.GetProperty("conversationId").GetString()!;
AgentSession byServiceId = await agent.CreateSessionAsync(conversationId);
Console.WriteLine(await agent.RunAsync("Summarize what you know about my trip in one sentence.", byServiceId));
