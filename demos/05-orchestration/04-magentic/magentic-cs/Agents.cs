using System.ClientModel.Primitives;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

static class Agents
{
    static readonly DefaultAzureCredential Credential = new();
    static readonly string Endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!;
    static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static AIAgent ResponsesAgent(string name, string instructions, string? description = null, IList<AITool>? tools = null) =>
        new AIProjectClient(new Uri(Endpoint), Credential)
            .AsAIAgent(model: Model, instructions: instructions, name: name, description: description, tools: tools);

    public static AIAgent ChatCompletionsAgent(string name, string instructions, string? description = null, IList<AITool>? tools = null)
    {
        OpenAIClient client = new(
            new BearerTokenPolicy(Credential, "https://ai.azure.com/.default"),
            new OpenAIClientOptions { Endpoint = new Uri($"https://{new Uri(Endpoint).Host}/openai/v1/") });
        return client.GetChatClient(Model).AsAIAgent(instructions: instructions, name: name, description: description, tools: tools);
    }
}
