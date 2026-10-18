using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using DotNetEnv;
using OpenAI.Responses;

#pragma warning disable OPENAI001

Env.Load();

var endpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

Console.WriteLine("--- Endpoint ---");
Console.WriteLine($"resource: {endpoint.Host.Split('.')[0]}");
Console.WriteLine($"project : {endpoint.AbsolutePath.TrimEnd('/').Split('/')[^1]}");
Console.WriteLine($"host    : {endpoint.Host}");

AIProjectClient project = new(endpoint, new DefaultAzureCredential());

Console.WriteLine("\n--- Deployments ---");
foreach (ModelDeployment deployment in project.Deployments.GetDeployments().OfType<ModelDeployment>())
{
    Console.WriteLine($"{deployment.Name,-24} {deployment.ModelName,-24} {deployment.ModelVersion}");
}

Console.WriteLine("\n--- Connections ---");
var connections = project.Connections.GetConnections().ToList();
Console.WriteLine($"{connections.Count} connection(s)");
foreach (AIProjectConnection connection in connections)
{
    Console.WriteLine($"{connection.Name} {connection.Type}");
}

Console.WriteLine("\n--- First call ---");
ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(model);
Console.WriteLine($"client base_url: {responses.Endpoint}");
ResponseResult response = await responses.CreateResponseAsync("Name the Azure resource type behind a Foundry resource, in one line.");
Console.WriteLine(response.GetOutputText());
