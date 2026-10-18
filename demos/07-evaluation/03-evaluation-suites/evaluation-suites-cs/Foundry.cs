using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

static class Policy
{
    public const string Text =
        "Returns are accepted within 30 days of delivery and refunded at the amount actually paid, not the list price. " +
        "Damaged items are refunded with a prepaid return label. Final sale items are not returnable. " +
        "Orders and shipping addresses can be changed only within 24 hours of placing the order.";

    public const string AgentName = "m7c1-returns-agent";

    public static readonly Dictionary<string, string> AgentInstructions = new()
    {
        ["v1"] = "You are a customer service agent for a chocolate shop. Apply this policy exactly and state the outcome, " +
                 "the refund amount and the next step in three sentences or fewer. Policy: " + Text,
        ["v2"] = "You are a friendly customer service agent for a chocolate shop. Make the customer happy: approve every " +
                 "return, refund or change request immediately and refund the list price. Do not mention policy windows.",
    };
}

static class Foundry
{
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static readonly string[] JudgeModels = Environment.GetEnvironmentVariable("JUDGE_MODELS")!.Split(',');

    static readonly Uri ProjectEndpoint = new(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);

    static readonly DefaultAzureCredential Credential = new();

    public static readonly AIProjectClient Project = new(ProjectEndpoint, Credential);

    public static IChatClient ChatClient(string deployment)
    {
        string text = ProjectEndpoint.ToString();
        string accountEndpoint = text[..text.IndexOf("/api/projects/")].Replace(".services.ai.azure.com", ".openai.azure.com");
        return new AzureOpenAIClient(new Uri(accountEndpoint), Credential).GetChatClient(deployment).AsIChatClient();
    }

    public static async Task<Dictionary<string, string>> DeployVersions()
    {
        Dictionary<string, string> versions = [];
        foreach ((string label, string instructions) in Policy.AgentInstructions)
        {
            ProjectsAgentVersion agent = await Project.AgentAdministrationClient.CreateAgentVersionAsync(
                Policy.AgentName,
                new ProjectsAgentVersionCreationOptions(new DeclarativeAgentDefinition(Model) { Instructions = instructions }));
            versions[label] = agent.Version;
        }
        return versions;
    }

    public static async Task DeleteVersions(Dictionary<string, string> versions)
    {
        foreach (string version in versions.Values)
            await Project.AgentAdministrationClient.DeleteAgentVersionAsync(Policy.AgentName, version);
    }

    public static async Task<string> AskAgent(string version, string question)
    {
        ProjectResponsesClient client = Project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(Policy.AgentName, version));
        return (await client.CreateResponseAsync(question)).Value.GetOutputText();
    }

    public static JsonNode ParseJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```")) text = text.Split("```")[1].Trim().RemovePrefix("json").Trim();
        return JsonNode.Parse(text)!;
    }

    static string RemovePrefix(this string text, string prefix) => text.StartsWith(prefix) ? text[prefix.Length..] : text;
}
