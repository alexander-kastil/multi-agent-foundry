using ModelContextProtocol.Client;

public static class AgentLifecycle
{
    private const string Agent = "m1-mcp-agent";

    public static async Task Run()
    {
        var endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");
        var definition = new Dictionary<string, object?>
        {
            ["kind"] = "prompt",
            ["model"] = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME"),
            ["instructions"] = "You explain Microsoft Foundry concepts in one short sentence and always call the product Microsoft Foundry.",
        };

        await using McpClient client = await FoundryMcp.Connect();

        var created = await FoundryMcp.Call(client, "agent_update", new()
        {
            ["projectEndpoint"] = endpoint,
            ["agentName"] = Agent,
            ["agentDefinition"] = definition,
        });
        var version = created.GetProperty("versions").GetProperty("latest");
        Console.WriteLine($"created   : {created.GetProperty("id")} version {version.GetProperty("version")} ({version.GetProperty("definition").GetProperty("kind")})");

        var reply = await FoundryMcp.Call(client, "agent_invoke", new()
        {
            ["projectEndpoint"] = endpoint,
            ["agentName"] = Agent,
            ["inputText"] = "What is a Foundry project?",
        });
        Console.WriteLine($"status    : {reply.GetProperty("status")}");
        Console.WriteLine($"answer    : {reply.GetProperty("output")[0].GetProperty("content")[0].GetProperty("text")}");

        var deleted = await FoundryMcp.Call(client, "agent_delete", new() { ["projectEndpoint"] = endpoint, ["agentName"] = Agent });
        Console.WriteLine($"deleted   : {deleted.GetProperty("deleted")}");

        var lookup = await client.CallToolAsync("agent_get", new Dictionary<string, object?> { ["projectEndpoint"] = endpoint, ["agentName"] = Agent });
        var lookupText = lookup.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().First().Text;
        Console.WriteLine($"lookup    : {(lookupText.Contains("not_found") ? "not found" : "still present")}");
    }
}
