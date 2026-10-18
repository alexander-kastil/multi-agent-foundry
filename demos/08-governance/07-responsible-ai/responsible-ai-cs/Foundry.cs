using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;

static class Foundry
{
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    static readonly AIProjectClient Project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());

    public static async Task<ProjectsAgentVersion> CreateAgent(string name, string instructions, Dictionary<string, string>? metadata = null)
    {
        ProjectsAgentVersionCreationOptions options = new(new DeclarativeAgentDefinition(Model) { Instructions = instructions });
        foreach (var (key, value) in metadata ?? []) options.Metadata[key] = value;
        return await Project.AgentAdministrationClient.CreateAgentVersionAsync(name, options);
    }

    public static async Task<string> Ask(ProjectsAgentVersion agent, string text)
    {
        ProjectResponsesClient client = Project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version));
        return (await client.CreateResponseAsync(text)).Value.GetOutputText();
    }

    public static Task DeleteAgent(ProjectsAgentVersion agent) =>
        Project.AgentAdministrationClient.DeleteAgentVersionAsync(agent.Name, agent.Version);
}
