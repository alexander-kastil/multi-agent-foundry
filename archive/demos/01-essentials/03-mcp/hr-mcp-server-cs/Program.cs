using HRMCPServer.Data;
using HRMCPServer.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Configure the employee database, preferring SQL Server when explicitly requested and falling back to SQLite otherwise
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider");
var connectionString = builder.Configuration.GetConnectionString("EmployeeDatabase");

builder.Services.AddDbContext<EmployeeDbContext>(options =>
{
    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
        return;
    }

    var sqliteConnectionString = BuildSqliteConnectionString(connectionString, builder.Environment.ContentRootPath);
    options.UseSqlite(sqliteConnectionString);
});

// Register the employee service
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Add the MCP services: the transport to use (HTTP) and the tools to register.
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Ensure database is created and seeded
await EmployeeDbInitializer.InitializeAsync(app.Services);

// Configure the application to use the MCP server
app.MapMcp();

// Run the application
// This will start the MCP server and listen for incoming requests.
app.Run();

static string BuildSqliteConnectionString(string? configuredValue, string contentRoot)
{
    const string dataSourceKey = "Data Source=";
    const string fileNameKey = "Filename=";

    if (string.IsNullOrWhiteSpace(configuredValue))
    {
        var defaultPath = Path.Combine(contentRoot, "employee.db");
        return $"{dataSourceKey}{defaultPath}";
    }

    var trimmed = configuredValue.Trim();

    if (trimmed.StartsWith(dataSourceKey, StringComparison.OrdinalIgnoreCase))
    {
        var value = trimmed[dataSourceKey.Length..].Trim().Trim('"');
        var resolvedPath = ResolvePath(value, contentRoot);
        return $"{dataSourceKey}{resolvedPath}";
    }

    if (trimmed.StartsWith(fileNameKey, StringComparison.OrdinalIgnoreCase))
    {
        var value = trimmed[fileNameKey.Length..].Trim().Trim('"');
        var resolvedPath = ResolvePath(value, contentRoot);
        return $"{dataSourceKey}{resolvedPath}";
    }

    var filePath = ResolvePath(trimmed, contentRoot);
    return $"{dataSourceKey}{filePath}";
}

static string ResolvePath(string path, string contentRoot)
{
    if (Path.IsPathRooted(path))
    {
        return path;
    }

    return Path.Combine(contentRoot, path);
}
