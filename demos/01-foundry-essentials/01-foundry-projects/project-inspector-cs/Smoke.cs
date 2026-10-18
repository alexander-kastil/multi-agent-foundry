using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class Smoke
{
    public static async Task Run()
    {
        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        ProjectResponsesClient responses = project.ProjectOpenAIClient
            .GetProjectResponsesClientForModel(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!);

        ResponseResult response = await responses.CreateResponseAsync("Reply with the single word: ready");

        Console.WriteLine($"base_url : {responses.Endpoint}");
        Console.WriteLine($"model    : {response.Model}");
        Console.WriteLine($"output   : {response.GetOutputText()}");
    }
}
