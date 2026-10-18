using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();

AIProjectClient project = new(
    endpoint: new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    tokenProvider: new DefaultAzureCredential());

McpTool browser = ResponseTool.CreateMcpTool(
    serverLabel: "playwrightWorkspace",
    serverUri: new Uri(Environment.GetEnvironmentVariable("PLAYWRIGHT_MCP_URL")!),
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
browser.ProjectConnectionId = Environment.GetEnvironmentVariable("PLAYWRIGHT_MCP_CONNECTION")!;

DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
{
    Instructions = "You automate browser tasks with the Browser Automation tool. "
        + "Create one browser session, reuse it for every step, and close it when the task is done. "
        + "Base every answer on what the browser shows, never on prior knowledge.",
    Tools = { browser },
};
ProjectsAgentVersion agent = project.AgentAdministrationClient.CreateAgentVersion(
    "m3-browser-agent", new ProjectsAgentVersionCreationOptions(definition));
Console.WriteLine($"Created {agent.Name}:{agent.Version}");

ProjectConversation conversation = project.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversation();
ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(
    new AgentReference(agent.Name, agent.Version), conversation);

string[] tasks =
[
    "Create a browser session and give me the live view link. Do not browse anywhere yet.",
    "Open https://example.com in that session. Report the page title and the description paragraph.",
    "Follow the link in the description paragraph and report the heading of the page it leads to. Then close the browser session.",
];

for (int i = 0; i < tasks.Length; i++)
{
    Console.WriteLine($"== Task {i + 1}: {tasks[i]}");
    ResponseResult response = responses.CreateResponse(tasks[i]);
    foreach (ResponseItem item in response.OutputItems)
    {
        Console.WriteLine($"item: {item.GetType().Name}");
    }
    string text = response.GetOutputText();
    Console.WriteLine(text.Contains("live.playwright.microsoft.com") ? "agent: live view URL received (kept private)" : $"agent: {text}");
}

project.AgentAdministrationClient.DeleteAgentVersion(agent.Name, agent.Version);
Console.WriteLine($"Deleted {agent.Name}:{agent.Version}");
