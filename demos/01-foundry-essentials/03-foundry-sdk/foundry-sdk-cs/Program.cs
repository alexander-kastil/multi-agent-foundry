using Azure.AI.Projects;
using Azure.Identity;
using DotNetEnv;
using Microsoft.Extensions.AI;

Env.Load();

var endpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

AIProjectClient project = new(endpoint, new DefaultAzureCredential());

IChatClient client = project
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(model);

var scenario = args.FirstOrDefault() ?? "chat";

switch (scenario)
{
    case "chat":
        await Chat.Run(client);
        break;
    case "stream":
        await Chat.Stream(client);
        break;
    case "tools":
        await Tools.Run(client);
        break;
}
