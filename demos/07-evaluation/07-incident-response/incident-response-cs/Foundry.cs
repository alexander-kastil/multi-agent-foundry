using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Responses;

static class Foundry
{
    public const string PricingAgentName = "m7c3-pricing-agent";

    public static readonly Dictionary<string, string> PricingPrompts = new()
    {
        ["v1"] = "You are a pricing agent. Given an order total and a discount percent, compute the final price. " +
                 "Reply with the numeric value only, with two decimals, no currency symbol and no other text.",
        ["v2"] = "You are a pricing agent. Given an order total and a discount percent, compute the final price. " +
                 "Always include the currency symbol and add a one sentence explanation of the discount for clarity.",
    };

    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    static readonly AIProjectClient Project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());

    public static async Task<Dictionary<string, string>> DeployPricingVersions()
    {
        Dictionary<string, string> versions = [];
        foreach ((string label, string prompt) in PricingPrompts)
        {
            ProjectsAgentVersion agent = await Project.AgentAdministrationClient.CreateAgentVersionAsync(
                agentName: PricingAgentName,
                options: new(new DeclarativeAgentDefinition(Model) { Instructions = prompt }));
            versions[label] = agent.Version;
        }
        return versions;
    }

    public static async Task<string> ReadInstructions(string version)
    {
        ProjectsAgentVersion agent = await Project.AgentAdministrationClient.GetAgentVersionAsync(PricingAgentName, version);
        return ((DeclarativeAgentDefinition)agent.Definition).Instructions;
    }

    public static async Task DeletePricingVersions(Dictionary<string, string> versions)
    {
        foreach (string version in versions.Values)
            await Project.AgentAdministrationClient.DeleteAgentVersionAsync(agentName: PricingAgentName, agentVersion: version);
    }

    public static async Task<string> AskPricingAgent(string version, string text)
    {
        ProjectResponsesClient client = Project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(PricingAgentName, version));
        return (await client.CreateResponseAsync(text)).Value.GetOutputText();
    }

    public static async Task<string> AskModel(string deployment, string instructions, string text)
    {
        CreateResponseOptions options = new() { Instructions = instructions, InputItems = { ResponseItem.CreateUserMessageItem(text) } };
        return (await Project.ProjectOpenAIClient.GetProjectResponsesClientForModel(deployment).CreateResponseAsync(options)).Value.GetOutputText();
    }
}
