using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();

string mcpEndpoint = $"{Environment.GetEnvironmentVariable("SEARCH_ENDPOINT")}/knowledgebases/{Environment.GetEnvironmentVariable("KNOWLEDGE_BASE")}/mcp?api-version=2026-08-01-preview";

AIProjectClient project = new(
    endpoint: new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    tokenProvider: new DefaultAzureCredential());

McpTool knowledgeBase = ResponseTool.CreateMcpTool(
    serverLabel: "knowledge-base",
    serverUri: new Uri(mcpEndpoint),
    allowedTools: new McpToolFilter { ToolNames = { "knowledge_base_retrieve" } },
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
knowledgeBase.ProjectConnectionId = Environment.GetEnvironmentVariable("KNOWLEDGE_BASE_CONNECTION")!;

DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
{
    Instructions = """
        You are a helpful assistant for Contoso Outdoor.
        Use the knowledge base tool to answer every question.
        If the knowledge base does not contain the answer, respond with "I don't know".
        When you use information from the knowledge base, include citations to the retrieved sources.
        """,
    Tools = { knowledgeBase },
};
ProjectsAgentVersion agent = project.AgentAdministrationClient.CreateAgentVersion(
    "m3-outdoor-agent-cs", new ProjectsAgentVersionCreationOptions(definition));
Console.WriteLine($"Created {agent.Name}:{agent.Version}");

ProjectConversation conversation = project.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversation();
ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(
    new AgentReference(agent.Name, agent.Version), conversation);

foreach (string question in new[]
{
    "Which of your tents is rated for winter storms, and what does it cost?",
    "If that tent arrives damaged, what must I do and how long do I have?",
})
{
    ResponseResult response = responses.CreateResponse(question);
    Console.WriteLine($"\nuser: {question}");
    Console.WriteLine($"items: {string.Join(", ", response.OutputItems.Select(i => i.GetType().Name))}");
    Console.WriteLine($"agent: {response.GetOutputText()}");
}

project.AgentAdministrationClient.DeleteAgentVersion(agent.Name, agent.Version);
Console.WriteLine($"\nDeleted {agent.Name}:{agent.Version}");
