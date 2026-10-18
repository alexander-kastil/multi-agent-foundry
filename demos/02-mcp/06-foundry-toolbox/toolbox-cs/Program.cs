using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

var projectEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "gpt-5.4";
var toolboxName = Environment.GetEnvironmentVariable("TOOLBOX_NAME")
    ?? throw new InvalidOperationException("TOOLBOX_NAME is not set.");

var credential = new DefaultAzureCredential();
AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), credential)
    .AsAIAgent(
        model: deploymentName,
        instructions: "Answer from Microsoft Learn and keep the snippets collection current. Keep answers under 60 words.",
        name: "m2-toolbox-host");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.Services.AddFoundryToolboxes(credential, toolboxName);

var app = builder.Build();
app.MapFoundryResponses();
app.Run();
