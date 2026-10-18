using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class RouteOnce
{
    public static async Task Run()
    {
        var deployment = Environment.GetEnvironmentVariable("MODEL_ROUTER_DEPLOYMENT_NAME")!;

        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(deployment);
        ResponseResult response = await responses.CreateResponseAsync("In one sentence, name the most popular tourist destination in Seattle.");

        Console.WriteLine($"deployment  : {deployment}");
        Console.WriteLine($"served by   : {response.Model}");
        Console.WriteLine($"answer      : {response.GetOutputText()}");
    }
}
