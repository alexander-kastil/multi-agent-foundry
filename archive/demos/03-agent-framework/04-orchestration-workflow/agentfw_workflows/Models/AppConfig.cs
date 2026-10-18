using Microsoft.Extensions.Configuration;

namespace AgentFwWorkflows.Models;

public class AppConfig
{
    public string AzureAIProjectEndpoint { get; set; } = string.Empty;
    public string AzureAIModelDeploymentName { get; set; } = string.Empty;
    public string DataPath { get; set; } = "./data";
    public string OutputPath { get; set; } = "./output";

    public static AppConfig FromConfiguration(IConfiguration configuration)
    {
        return new AppConfig
        {
            AzureAIProjectEndpoint = configuration["AzureAIProjectEndpoint"] ?? string.Empty,
            AzureAIModelDeploymentName = configuration["AzureAIModelDeploymentName"] ?? string.Empty,
            DataPath = configuration["DataPath"] ?? "./data",
            OutputPath = configuration["OutputPath"] ?? "./output"
        };
    }
}
