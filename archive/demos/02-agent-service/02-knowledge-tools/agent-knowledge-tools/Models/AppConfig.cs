using Microsoft.Extensions.Configuration;

namespace AgentKnowledgeTools.Models;

public class AppConfig
{
    public string ProjectConnectionString { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? FunctionDeploymentUrl { get; set; }
    public string? BingConnection { get; set; }
    public string? SharePointConnection { get; set; }
    public string? AzurePlaywrightConnectionId { get; set; }
    public string? ComputerUseEnvironment { get; set; }
    public string? RestUrl { get; set; }
    public string? McpServerUrl { get; set; }
    public string? McpServerLabel { get; set; }
    public string? AzureAiSearchConnection { get; set; }
    public string? AzureAiIndexName { get; set; }
    public string? OnVistaUrl { get; set; }
    public string? OutputPath { get; set; }
    public bool DetailedLogging { get; set; }
    public bool DeleteAgentOnExit { get; set; }
    public string? VectorStoreId { get; set; }

    public static AppConfig FromConfiguration(IConfiguration configuration)
    {
        var cfg = new AppConfig
        {
            ProjectConnectionString = configuration["ProjectConnectionString"] ?? throw new InvalidOperationException("ProjectConnectionString is required in appsettings.json"),
            Model = configuration["Model"] ?? throw new InvalidOperationException("Model is required in appsettings.json"),
            FunctionDeploymentUrl = configuration["FunctionDeploymentUrl"],
            BingConnection = configuration["BingConnection"],
            SharePointConnection = configuration["SharePointConnection"],
            AzurePlaywrightConnectionId = configuration["AzurePlaywrightConnectionId"],
            ComputerUseEnvironment = configuration["ComputerUseEnvironment"],
            RestUrl = configuration["RestUrl"],
            McpServerUrl = configuration["McpServerUrl"],
            McpServerLabel = configuration["McpServerLabel"],
            AzureAiSearchConnection = configuration["AzureAiSearchConnection"],
            AzureAiIndexName = configuration["AzureAiIndexName"],
            OnVistaUrl = configuration["OnVistaUrl"],
            OutputPath = configuration["OutputPath"],
            DetailedLogging = bool.TryParse(configuration["DetailedLogging"], out var detailedLogging) && detailedLogging,
            DeleteAgentOnExit = bool.TryParse(configuration["DeleteAgentOnExit"], out var deleteOnExit) && deleteOnExit,
            VectorStoreId = configuration["VECTOR_STORE_ID"]
        };

        return cfg;
    }
}
