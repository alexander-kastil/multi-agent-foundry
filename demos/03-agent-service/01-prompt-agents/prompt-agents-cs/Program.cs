using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();

string agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "m3-support-agent";
string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

AIProjectClient project = new(
    endpoint: new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    tokenProvider: new DefaultAzureCredential());

async Task<ProjectsAgentVersion> CreateVersion(string instructions)
{
    ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync(
        agentName: agentName,
        options: new(new DeclarativeAgentDefinition(model) { Instructions = instructions }));
    Console.WriteLine($"Created {agent.Name}:{agent.Version}");
    return agent;
}

Console.WriteLine("== Step 1: create version 1");
ProjectsAgentVersion v1 = await CreateVersion("You are a support agent for Contoso Outdoor. Answer in one short sentence.");

Console.WriteLine("== Step 2: single response, no conversation");
ProjectResponsesClient single = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(v1.Name, v1.Version));
ResponseResult first = await single.CreateResponseAsync("My tent zipper is stuck. What should I do?");
Console.WriteLine($"{agentName}:{v1.Version} -> {first.GetOutputText()}");

Console.WriteLine("== Step 3: conversation with history");
ProjectConversation conversation = await project.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversationAsync();
Console.WriteLine($"Conversation: {conversation.Id}");
ProjectResponsesClient chat = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(v1.Name, v1.Version), conversation);
foreach (string question in new[] { "I bought the Trailhead 2 tent last week.", "Which tent did I buy?" })
{
    ResponseResult turn = await chat.CreateResponseAsync(question);
    Console.WriteLine($"user: {question}\nagent: {turn.GetOutputText()}");
}

Console.WriteLine("== Step 4: create version 2 and continue the same conversation");
ProjectsAgentVersion v2 = await CreateVersion("You are a support agent for Contoso Outdoor. Answer in two short sentences and end with a follow-up question.");
ProjectResponsesClient chatV2 = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(v2.Name, v2.Version), conversation);
ResponseResult second = await chatV2.CreateResponseAsync("Summarize what we discussed.");
Console.WriteLine($"{agentName}:{v2.Version} -> {second.GetOutputText()}");

Console.WriteLine("== Cleanup");
foreach (ProjectsAgentVersion version in new[] { v1, v2 })
{
    await project.AgentAdministrationClient.DeleteAgentVersionAsync(agentName: version.Name, agentVersion: version.Version);
    Console.WriteLine($"Deleted {version.Name}:{version.Version}");
}
