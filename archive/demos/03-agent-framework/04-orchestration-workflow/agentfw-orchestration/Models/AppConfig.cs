using Microsoft.Extensions.Configuration;

namespace AFWOrchestration;

public class OrchestrationAppConfig
{
    public string ProjectConnectionString { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OutcomeDirectory { get; set; } = "outcomes";
    public string LogDirectory { get; set; } = "logs";

    public static OrchestrationAppConfig FromConfiguration(IConfiguration configuration)
    {
        var cfg = new OrchestrationAppConfig
        {
            ProjectConnectionString = configuration["ProjectConnectionString"] ?? throw new InvalidOperationException("ProjectConnectionString is required in appsettings.json"),
            Model = configuration["Model"] ?? throw new InvalidOperationException("Model is required in appsettings.json"),
            OutcomeDirectory = configuration["OutcomeDirectory"] ?? "outcomes",
            LogDirectory = configuration["LogDirectory"] ?? "logs"
        };

        return cfg;
    }
}
