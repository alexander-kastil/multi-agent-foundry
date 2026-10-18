using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;

public static class Session
{
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static AIProjectClient Project { get; } = new(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential());

    public static ProjectResponsesClient Responses(string? conversationId = null) =>
        Project.ProjectOpenAIClient.GetProjectResponsesClientForModel(Model, conversationId);

    public static ProjectConversationsClient Conversations =>
        Project.ProjectOpenAIClient.GetProjectConversationsClient();
}
