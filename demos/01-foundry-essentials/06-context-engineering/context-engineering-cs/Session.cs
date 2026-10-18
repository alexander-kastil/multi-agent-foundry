using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;

public static class Session
{
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static AIProjectClient Project { get; } = new(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential());

    public static ProjectResponsesClient Responses() =>
        Project.ProjectOpenAIClient.GetProjectResponsesClientForModel(Model);
}
