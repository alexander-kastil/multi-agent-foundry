using System.Diagnostics;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpResilience;

public static class Probe
{
    private const string Base = "http://127.0.0.1:47030";

    public static async Task RunAsync()
    {
        var apiKey = Environment.GetEnvironmentVariable("MCP_API_KEY") ?? "change-me";
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-api-key", apiKey);

        using var anonymous = new HttpClient();
        Console.WriteLine($"no key   -> {(int)(await anonymous.GetAsync($"{Base}/health")).StatusCode}");
        Console.WriteLine($"with key -> {(int)(await http.GetAsync($"{Base}/health")).StatusCode}");

        await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{Base}/mcp"),
            AdditionalHeaders = new Dictionary<string, string> { ["x-api-key"] = apiKey }
        }));

        Console.WriteLine("\n=== Healthy ===");
        Console.WriteLine(await Call(client, "warfarin", "aspirin"));

        Console.WriteLine("\n=== Outage: retries, then the circuit opens ===");
        await http.PostAsync($"{Base}/admin/outage?on=true", null);
        for (var i = 1; i <= 4; i++)
        {
            var watch = Stopwatch.StartNew();
            var result = await Call(client, "atorvastatin", "clarithromycin");
            Console.WriteLine($"call {i}: {watch.ElapsedMilliseconds} ms {result}");
        }
        Console.WriteLine(await http.GetStringAsync($"{Base}/health"));

        Console.WriteLine("\n=== Recovery ===");
        await http.PostAsync($"{Base}/admin/outage?on=false", null);
        await Task.Delay(TimeSpan.FromSeconds(10.5));
        Console.WriteLine(await Call(client, "atorvastatin", "clarithromycin"));
        Console.WriteLine(await http.GetStringAsync($"{Base}/health"));
    }

    private static async Task<string> Call(McpClient client, string drugA, string drugB)
    {
        var result = await client.CallToolAsync("lookup_drug_interaction", new Dictionary<string, object?> { ["drugA"] = drugA, ["drugB"] = drugB });
        return result.Content.OfType<TextContentBlock>().First().Text;
    }
}
