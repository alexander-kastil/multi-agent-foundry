using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var endpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
var deployment = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

AIAgent writer = new AIProjectClient(endpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You write short product release announcements. Return JSON with title and body.",
        name: "AnnouncementWriter");

using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options => options.AddAIAgent(writer))
    .Build();

app.Run();
