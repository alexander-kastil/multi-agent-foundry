using System.ClientModel;
using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

static class OAuthConsent
{
    const string Name = "m8h-idn-github";

    public static async Task Run()
    {
        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());

        McpTool github = ResponseTool.CreateMcpTool(
            serverLabel: "github",
            serverUri: new Uri("https://api.githubcopilot.com/mcp/"),
            toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
        github.ProjectConnectionId = Environment.GetEnvironmentVariable("OAUTH_CONNECTION_NAME")!;

        DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
        {
            Instructions = "Use the GitHub tool to answer.",
            Tools = { github },
        };
        ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync(Name, new ProjectsAgentVersionCreationOptions(definition));

        ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version));
        ClientResult<ResponseResult> result = await responses.CreateResponseAsync("Which GitHub user am I?");
        JsonNode body = JsonNode.Parse(result.GetRawResponse().Content)!;
        foreach (JsonNode? item in body["output"]!.AsArray())
        {
            Console.WriteLine($"output item: {item!["type"]}");
            if ((string?)item["type"] == "oauth_consent_request")
            {
                Console.WriteLine($"  consent host: {new Uri((string)item["consent_link"]!).Host}");
                Console.WriteLine($"  response id:  {body["id"]}");
            }
        }

        await project.AgentAdministrationClient.DeleteAgentVersionAsync(agent.Name, agent.Version);
    }
}
