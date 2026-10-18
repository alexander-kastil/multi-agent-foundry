using System.Text.Json.Nodes;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

static class Agents
{
    static readonly AIProjectClient Project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
    static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    public static AIAgent Make(string name, string instructions, string? description = null) =>
        Project.AsAIAgent(model: Model, instructions: instructions, name: name, description: description);

    public static JsonNode ParseJson(string text)
    {
        string cleaned = text.Trim();
        if (cleaned.StartsWith("```"))
            cleaned = cleaned[(cleaned.IndexOf('\n') + 1)..cleaned.LastIndexOf("```")];
        return JsonNode.Parse(cleaned)!;
    }

    public static async Task<JsonNode> AskJson(AIAgent agent, string prompt) =>
        ParseJson((await agent.RunAsync(prompt)).Text);
}
