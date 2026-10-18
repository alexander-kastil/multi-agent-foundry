using Microsoft.Extensions.Configuration;
using System;

namespace ConnectedAgents.Models;

public class AiAppConfig
{
    public string ProjectConnectionString { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string AgentName { get; set; } = "ai-agent";

    public static AiAppConfig FromConfiguration(IConfiguration configuration)
    {
        var cfg = new AiAppConfig
        {
            ProjectConnectionString = configuration["ProjectConnectionString"] ?? throw new InvalidOperationException("ProjectConnectionString is required in appsettings.json"),
            Model = configuration["Model"] ?? throw new InvalidOperationException("Model is required in appsettings.json"),
            AgentName = configuration["AgentName"] ?? "ai-agent"
        };

        return cfg;
    }
}
