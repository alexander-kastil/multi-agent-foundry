using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

DotNetEnv.Env.TraversePath().Load();

var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var azureOpenAIEndpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!);
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
var agentName = Environment.GetEnvironmentVariable("FOUNDRY_AGENT_NAME")!;
var credential = new DefaultAzureCredential();

const string instructions = "You are a trail guide for Contoso Outdoor. Answer in two sentences.";
const string question = "What should I pack for a day hike in the Alps?";

var projectClient = new AIProjectClient(projectEndpoint, credential);

AIAgent foundryChatAgent = projectClient.AsAIAgent(
    model: model,
    instructions: instructions,
    name: "OutdoorGuide");
Console.WriteLine($"[FoundryChatClient] {await foundryChatAgent.RunAsync(question)}\n");

await foreach (var update in foundryChatAgent.RunStreamingAsync("Name three Austrian mountain ranges."))
{
    Console.Write(update);
}
Console.WriteLine("\n");

var azureOpenAI = new AzureOpenAIClient(azureOpenAIEndpoint, credential);
AIAgent chatCompletionAgent = azureOpenAI
    .GetChatClient(model)
    .AsIChatClient()
    .AsAIAgent(instructions: instructions, name: "OutdoorGuide");
Console.WriteLine($"[ChatClient] {await chatCompletionAgent.RunAsync(question)}\n");

FoundryAgent promptAgent = projectClient.AsAIAgent(new AgentReference(agentName, "1"));
Console.WriteLine($"[FoundryAgent] {await promptAgent.RunAsync("How do I return a tent?")}");
