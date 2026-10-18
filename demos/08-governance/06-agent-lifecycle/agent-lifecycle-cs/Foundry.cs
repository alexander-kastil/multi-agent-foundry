using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Json;
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
    const string Scope = "https://ai.azure.com/.default";

    const string FeatureHeader = "WorkflowAgents=V1Preview,ExternalAgents=V1Preview,DraftAgents=V1Preview,VoiceAgents=V1Preview,DigitalWorker=V1Preview,GitHubCopilot=V1Preview,Skills=V1Preview,MicrosoftCopilot=V1Preview,ModelRouterControls=V1Preview";

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

    public static ProjectResponsesClient ForAgent(ProjectsAgentVersion agent) =>
        Project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version));

    public static async Task<string> Ask(ProjectsAgentVersion agent, string text) =>
        (await ForAgent(agent).CreateResponseAsync(text)).Value.GetOutputText();

    static async Task<HttpClient> Http()
    {
        AccessToken token = await Credential.GetTokenAsync(new TokenRequestContext([Scope]));
        HttpClient http = new();
        http.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
        http.DefaultRequestHeaders.Add("Foundry-Features", FeatureHeader);
        return http;
    }

    public static async Task<string> AskEndpoint(string name, string text)
    {
        using HttpClient http = await Http();
        HttpResponseMessage reply = await http.PostAsJsonAsync($"{Endpoint}/agents/{name}/endpoint/protocols/openai/responses?api-version=v1", new { input = text });
        reply.EnsureSuccessStatusCode();
        JsonNode body = JsonNode.Parse(await reply.Content.ReadAsStringAsync())!;
        return string.Concat(body["output"]!.AsArray()
            .SelectMany(item => item!["content"]?.AsArray() ?? [])
            .Where(part => (string?)part!["type"] == "output_text")
            .Select(part => (string)part!["text"]!));
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
        using HttpClient http = await Http();
        using StringContent content = new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/merge-patch+json");
        HttpResponseMessage response = await http.PatchAsync($"{Endpoint}/agents/{name}?api-version=v1", content);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> ServedVersion(string name)
    {
        ClientResult agent = await Project.AgentAdministrationClient.GetAgentAsync(name, new RequestOptions());
        JsonNode body = JsonNode.Parse(agent.GetRawResponse().Content)!;
        JsonNode? selector = body["agent_endpoint"]?["version_selector"]?["version_selection_rules"]?[0]?["agent_version"];
        return (string?)selector ?? "@latest";
    }

    public static Task DeleteAgent(ProjectsAgentVersion agent) =>
        Project.AgentAdministrationClient.DeleteAgentVersionAsync(agent.Name, agent.Version);
}
