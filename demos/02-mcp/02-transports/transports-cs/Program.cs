using ModelContextProtocol.Client;
using Transports;

if (args.Contains("--client"))
{
    var mode = args.Last();
    IClientTransport transport = mode == "http"
        ? new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri("http://127.0.0.1:47006/mcp") })
        : new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = ["run", "--no-build", "--project", ".", "--", "--stdio"]
        });

    await using var client = await McpClient.CreateAsync(transport);
    var info = await client.CallToolAsync("server_info");
    Console.WriteLine($"{mode} -> {info.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().First().Text}");
    var sum = await client.CallToolAsync("add", new Dictionary<string, object?> { ["a"] = 2, ["b"] = 3 });
    Console.WriteLine($"add -> {sum.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().First().Text}");
    return;
}

if (args.Contains("--stdio"))
{
    LabTools.Transport = "stdio";
    var host = Host.CreateApplicationBuilder(args);
    host.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    host.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
    await host.Build().RunAsync();
    return;
}

LabTools.Transport = "http";
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();
var app = builder.Build();
app.MapMcp("/mcp");
app.Run("http://127.0.0.1:47006");
