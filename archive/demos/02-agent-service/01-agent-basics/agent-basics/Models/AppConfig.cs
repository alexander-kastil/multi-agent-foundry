using Microsoft.Extensions.Configuration;

namespace AgentBasics.Models;

public class AppConfig
{
    public string ProjectConnectionString { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? StorageConnectionString { get; set; }
    public string? StorageContainerName { get; set; }

    public static AppConfig FromConfiguration(IConfiguration configuration)
    {
        var cfg = new AppConfig
        {
            ProjectConnectionString = configuration["ProjectConnectionString"] ?? throw new InvalidOperationException("ProjectConnectionString is required in appsettings.json"),
            Model = configuration["Model"] ?? throw new InvalidOperationException("Model is required in appsettings.json"),
            StorageConnectionString = configuration["StorageConnectionString"],
            StorageContainerName = configuration["StorageContainerName"]
        };

        return cfg;
    }
}
