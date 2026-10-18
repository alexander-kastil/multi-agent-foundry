using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using OpenAI.Responses;

static class Foundry
{
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static readonly string Endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!.TrimEnd('/');

    static readonly DefaultAzureCredential Credential = new();

    public static readonly AIProjectClient Project = new(new Uri(Endpoint), Credential);

    public static async Task<ProjectsAgentVersion> CreateAgent(string name, string instructions, Dictionary<string, string>? metadata = null)
    {
        ProjectsAgentVersionCreationOptions options = new(new DeclarativeAgentDefinition(Model) { Instructions = instructions });
        foreach (var (key, value) in metadata ?? []) options.Metadata[key] = value;
        return await Project.AgentAdministrationClient.CreateAgentVersionAsync(name, options);
    }

    public static async Task<string> Ask(string name, string version, string text)
    {
        ProjectResponsesClient client = Project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(name, version));
        ResponseResult response = await client.CreateResponseAsync(text);
        return response.GetOutputText();
    }

    public static async Task PinEndpoint(string name, string version)
    {
        var body = new
        {
            agent_endpoint = new
            {
                version_selector = new
                {
                    version_selection_rules = new[] { new { type = "FixedRatio", agent_version = version, traffic_percentage = 100 } },
                },
            },
        };
        AccessToken token = await Credential.GetTokenAsync(new TokenRequestContext(["https://ai.azure.com/.default"]));
        using HttpClient http = new();
        http.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
        using StringContent content = new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/merge-patch+json");
        HttpResponseMessage response = await http.PatchAsync($"{Endpoint}/agents/{name}?api-version=v1", content);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> ServedVersion(string name)
    {
        ClientResult agent = await Project.AgentAdministrationClient.GetAgentAsync(name, new RequestOptions());
        JsonNode body = JsonNode.Parse(agent.GetRawResponse().Content)!;
        return (string)body["agent_endpoint"]!["version_selector"]!["version_selection_rules"]![0]!["agent_version"]!;
    }

    public static Task DeleteAgent(string name, string version) =>
        Project.AgentAdministrationClient.DeleteAgentVersionAsync(name, version);
}
