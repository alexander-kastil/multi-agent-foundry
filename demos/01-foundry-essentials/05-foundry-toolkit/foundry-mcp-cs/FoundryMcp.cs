using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

public static class FoundryMcp
{
    private const string McpUrl = "https://mcp.ai.azure.com";

    public static Task<McpClient> Connect()
    {
        HttpClient http = new(new BearerTokenHandler(new DefaultAzureCredential(), $"{McpUrl}/.default"))
        {
            Timeout = TimeSpan.FromSeconds(120),
        };

        return McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(McpUrl),
                Name = "foundry-mcp-remote",
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http));
    }

    public static async Task<JsonElement> Call(McpClient client, string tool, Dictionary<string, object?> arguments)
    {
        CallToolResult result = await client.CallToolAsync(tool, arguments);
        var text = result.Content.OfType<TextContentBlock>().First().Text;
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}

public sealed class BearerTokenHandler(TokenCredential credential, string scope) : HttpClientHandler
{
    private readonly TokenRequestContext _context = new([scope]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(_context, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}
