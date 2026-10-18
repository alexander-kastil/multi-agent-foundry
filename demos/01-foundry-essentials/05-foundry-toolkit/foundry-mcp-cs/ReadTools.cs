using ModelContextProtocol.Client;

public static class ReadTools
{
    public static async Task Run(string deploymentName)
    {
        await using McpClient client = await FoundryMcp.Connect();

        var catalog = await FoundryMcp.Call(client, "model_catalog_list", new() { ["modelName"] = "model-router" });
        var entry = catalog[0];
        Console.WriteLine($"catalog   : {entry.GetProperty("name")} by {entry.GetProperty("publisher")}");
        Console.WriteLine($"asset     : {entry.GetProperty("assetId")}");

        var result = await FoundryMcp.Call(client, "model_deployment_get", new()
        {
            ["foundryAccountResourceId"] = Environment.GetEnvironmentVariable("FOUNDRY_ACCOUNT_RESOURCE_ID"),
            ["deploymentName"] = deploymentName,
        });
        var deployment = result[0];
        var data = deployment.GetProperty("data");
        var properties = data.GetProperty("properties");
        var model = properties.GetProperty("model");

        Console.WriteLine();
        Console.WriteLine($"deployment: {deployment.GetProperty("name")} ({deployment.GetProperty("kind")}, {deployment.GetProperty("provisioningState")})");
        Console.WriteLine($"model     : {model.GetProperty("name")} {model.GetProperty("version")}");
        Console.WriteLine($"capacity  : {data.GetProperty("sku").GetProperty("name")} x {properties.GetProperty("currentCapacity")}");
        foreach (var routed in properties.GetProperty("routing").GetProperty("models").EnumerateArray())
        {
            Console.WriteLine($"routes to : {routed.GetProperty("name")} {routed.GetProperty("version")}");
        }
    }
}
