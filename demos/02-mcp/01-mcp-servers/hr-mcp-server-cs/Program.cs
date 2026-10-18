using HrMcpServer.Models;
using ModelContextProtocol.Server;

if (args.Contains("--stdio"))
{
    var host = Host.CreateApplicationBuilder(args);
    host.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    host.Services.AddSingleton<EmployeeStore>();
    host.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly()
        .WithResourcesFromAssembly()
        .WithPromptsFromAssembly();
    await host.Build().RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<EmployeeStore>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

var app = builder.Build();
app.MapMcp("/mcp");
app.Run();
