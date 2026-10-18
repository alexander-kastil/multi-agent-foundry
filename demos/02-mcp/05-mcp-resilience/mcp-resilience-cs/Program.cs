using McpResilience;

if (args.Contains("--probe"))
{
    await Probe.RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DrugBackend>();
builder.Services.AddSingleton<ResilientLookup>();
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

var app = builder.Build();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapGet("/health", (ResilientLookup lookup) => new { circuit = lookup.State.ToString() });
app.MapPost("/admin/outage", (DrugBackend backend, bool on) =>
{
    backend.Outage = on;
    return new { outage = on };
});
app.MapMcp("/mcp");
app.Run("http://127.0.0.1:47030");
