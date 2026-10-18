using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;

#pragma warning disable OPENAI001

DotNetEnv.Env.Load();

var endpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

AIProjectClient project = new(endpoint, new DefaultAzureCredential());

IChatClient chat = project.ProjectOpenAIClient
    .GetProjectResponsesClientForModel(model)
    .AsIChatClient(model);

ChatResponse response = await chat.GetResponseAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful assistant that knows about gym, workouts, and fitness."),
    new ChatMessage(ChatRole.User, "What kinds of protein do you know about? Just a short list please.")
]);
Console.WriteLine($"[ASSISTANT]: {response.Text}");
