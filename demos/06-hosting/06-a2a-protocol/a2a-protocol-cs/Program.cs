using A2A;
using A2A.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

string endpoint = builder.Configuration["PROJECT_ENDPOINT"]
    ?? throw new InvalidOperationException("PROJECT_ENDPOINT is not set.");
string deployment = builder.Configuration["MODEL_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME is not set.");
string baseUrl = builder.Configuration["A2A_BASE_URL"] ?? "http://localhost:5000";

IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deployment);
builder.Services.AddSingleton(chatClient);

var titleAgent = builder.AddAIAgent(
    "title",
    instructions: "You write one catchy blog post title for the topic you are given. Reply with the title only.");
titleAgent.AddA2AServer();

var app = builder.Build();

app.MapA2AHttpJson(titleAgent, "/a2a/title");
app.MapA2AJsonRpc(titleAgent, "/a2a/title-rpc");

app.MapWellKnownAgentCard(new AgentCard
{
    Name = "m6h2-title-agent-cs",
    Description = "Writes blog post titles.",
    Version = "1.0",
    Skills =
    [
        new A2A.AgentSkill
        {
            Id = "generate_title",
            Name = "Generate title",
            Description = "Writes a blog post title for a topic.",
            Tags = ["title", "blog"],
        }
    ],
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url = $"{baseUrl}/a2a/title",
            ProtocolBinding = ProtocolBindingNames.HttpJson,
            ProtocolVersion = "1.0",
        },
        new AgentInterface
        {
            Url = $"{baseUrl}/a2a/title-rpc",
            ProtocolBinding = ProtocolBindingNames.JsonRpc,
            ProtocolVersion = "1.0",
        }
    ]
});

app.Run();
