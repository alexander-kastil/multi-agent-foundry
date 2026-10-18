using Microsoft.Extensions.Configuration;

namespace AgentFwBasics.Models;

public class AppConfig
{
    public string AzureAIProjectEndpoint { get; set; } = string.Empty;
    public string AzureAIModelDeploymentName { get; set; } = string.Empty;
    public string AzureOpenAIEndpoint { get; set; } = string.Empty;
    public string AzureOpenAIChatDeploymentName { get; set; } = string.Empty;
    public string AzureOpenAIApiKey { get; set; } = string.Empty;
    public string AzureOpenAIApiVersion { get; set; } = "2025-01-01-preview";
    public string DataPath { get; set; } = "./data";
    public string OutputPath { get; set; } = "./output";
    public string AzureAIAgentId { get; set; } = string.Empty;

    public static AppConfig FromConfiguration(IConfiguration configuration)
    {
        return new AppConfig
        {
            AzureAIProjectEndpoint = configuration["AzureAIProjectEndpoint"] ?? string.Empty,
            AzureAIModelDeploymentName = configuration["AzureAIModelDeploymentName"] ?? string.Empty,
            AzureOpenAIEndpoint = configuration["AzureOpenAIEndpoint"] ?? string.Empty,
            AzureOpenAIChatDeploymentName = configuration["AzureOpenAIChatDeploymentName"] ?? string.Empty,
            AzureOpenAIApiKey = configuration["AzureOpenAIApiKey"] ?? string.Empty,
            AzureOpenAIApiVersion = configuration["AzureOpenAIApiVersion"] ?? "2025-01-01-preview",
            DataPath = configuration["DataPath"] ?? "./data",
            OutputPath = configuration["OutputPath"] ?? "./output",
            AzureAIAgentId = configuration["AzureAIAgentId"] ?? string.Empty
        };
    }
}
