using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

DotNetEnv.Env.TraversePath().Load();

var azureOpenAI = new AzureOpenAIClient(
    new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!),
    new DefaultAzureCredential());

ChatClientAgent agent = azureOpenAI
    .GetChatClient(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "TrailGuide",
        ChatOptions = new() { Instructions = "You are a trail guide for Contoso Outdoor. Answer in two sentences." },
        ChatHistoryProvider = new InMemoryChatHistoryProvider(),
        AIContextProviders = [new PreferenceProvider()]
    });

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

var history = agent.GetService<InMemoryChatHistoryProvider>()!.GetMessages(session);
Console.WriteLine($"history messages: {history.Count}");

Directory.CreateDirectory("sessions");
JsonElement serialized = await agent.SerializeSessionAsync(session);
await File.WriteAllTextAsync(sessionFile, JsonSerializer.Serialize(serialized, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(serialized.GetProperty("stateBag").GetProperty("PreferenceProvider"));
