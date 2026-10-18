using ModelContextProtocol.Client;

public static class ListTools
{
    public static async Task Run()
    {
        await using McpClient client = await FoundryMcp.Connect();
        IList<McpClientTool> tools = await client.ListToolsAsync();

        Console.WriteLine($"tools exposed: {tools.Count}");
        foreach (var group in tools.GroupBy(tool => tool.Name.Split('_')[0]).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"{group.Key,-12} {group.Count()}");
        }

        Console.WriteLine();
        foreach (var tool in tools.Where(tool => tool.Name is "model_deployment_get" or "agent_update"))
        {
            Console.WriteLine(tool.Name);
            foreach (var property in tool.JsonSchema.GetProperty("properties").EnumerateObject())
            {
                var type = property.Value.TryGetProperty("type", out var value) ? value.ToString() : "object";
                Console.WriteLine($"  {property.Name,-28} {type}");
            }
        }
    }
}
