using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI;
using OpenAI.Embeddings;
using OpenAI.Responses;

static class Foundry
{
    static readonly string Endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!;
    static readonly AIProjectClient Project = new(new Uri(Endpoint), new DefaultAzureCredential());

    public static string Model(string name) => Environment.GetEnvironmentVariable(name)!;

    public static async Task<ResponseResult> Respond(string model, string input, string? instructions = null)
    {
        CreateResponseOptions options = new() { InputItems = { ResponseItem.CreateUserMessageItem(input) } };
        if (instructions is not null) options.Instructions = instructions;
        return await Project.ProjectOpenAIClient.GetProjectResponsesClientForModel(model).CreateResponseAsync(options);
    }

    public static EmbeddingClient? Embeddings()
    {
        string? deployment = Environment.GetEnvironmentVariable("EMBEDDING_DEPLOYMENT_NAME");
        if (string.IsNullOrEmpty(deployment)) return null;
        return new OpenAIClient(
            new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
            new OpenAIClientOptions { Endpoint = new Uri(Endpoint[..Endpoint.IndexOf("/api/projects")] + "/openai/v1/") })
            .GetEmbeddingClient(deployment);
    }
}
