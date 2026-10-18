using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();

AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
ProjectConversationsClient conversations = project.ProjectOpenAIClient.GetProjectConversationsClient();

if (args.FirstOrDefault() == "resume")
{
    Session saved = SessionStore.Get("analyst-session-1");
    Console.WriteLine($"Loaded session: conversation {saved.ConversationId}, metadata {string.Join(", ", saved.Metadata.Select(pair => $"{pair.Key}={pair.Value}"))}");
    LoopCore resumeLoop = new(project.ProjectOpenAIClient, new AgentReference(LoopCore.AgentName, saved.Metadata["agent_version"]));
    ResponseResult resumed = await resumeLoop.RunTurn("Which ticker did we just discuss, and what was its P/E ratio?", conversationId: saved.ConversationId);
    Console.WriteLine($"Resumed: {resumed.GetOutputText()}");
    await conversations.DeleteConversationAsync(saved.ConversationId);
    await project.AgentAdministrationClient.DeleteAgentVersionAsync(agentName: LoopCore.AgentName, agentVersion: saved.Metadata["agent_version"]);
    Console.WriteLine("Deleted conversation and agent version");
    return;
}

ProjectsAgentVersion agent = (await project.AgentAdministrationClient.CreateAgentVersionAsync(
    agentName: LoopCore.AgentName,
    options: new(new DeclarativeAgentDefinition(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
    {
        Instructions = LoopCore.Instructions,
        Tools = { LoopCore.MarketDataTool },
    }))).Value;
Console.WriteLine($"Agent {agent.Name} version {agent.Version}");
LoopCore loop = new(project.ProjectOpenAIClient, new AgentReference(agent.Name, agent.Version));

Console.WriteLine("\nPlan then act");
ResponseResult acted = await loop.PlanThenAct("Evaluate MSFT as a long-term investment");

Console.WriteLine("\nAct then reflect");
var (reflected, trace) = await loop.Reflect(acted);
Console.WriteLine($"Recommendation: {trace["recommendation"]}");

Console.WriteLine("\nFork into scenarios");
foreach (var (name, job) in await loop.ForkScenarios(reflected))
    Console.WriteLine($"[{name}] {job.GetOutputText()}");

Console.WriteLine("\nPersist the session");
ProjectConversation conversation = (await conversations.CreateProjectConversationAsync()).Value;
ResponseResult first = await loop.RunTurn("Fetch market data for GOOG and summarize it in one sentence.", conversationId: conversation.Id);
SessionStore.Save("analyst-session-1", conversation.Id, first.Id, new() { ["ticker"] = "GOOG", ["workflow"] = "research", ["agent_version"] = agent.Version });
Console.WriteLine($"Saved analyst-session-1 -> {conversation.Id}");
