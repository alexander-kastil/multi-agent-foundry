using Microsoft.Extensions.Configuration;
using System;

namespace ConnectedAgents.Models;

public class AppConfig
{
    public string ProjectConnectionString { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool VerboseOutput { get; set; }
    public bool CreateMermaidDiagram { get; set; }
    public string TicketFolderPath { get; set; } = "./tickets";
    public string OutputPath { get; set; } = "output";

    public static AppConfig FromConfiguration(IConfiguration configuration)
    {
        var cfg = new AppConfig
        {
            ProjectConnectionString = configuration["ProjectConnectionString"] ?? throw new InvalidOperationException("ProjectConnectionString is required in appsettings.json"),
            Model = configuration["Model"] ?? throw new InvalidOperationException("Model is required in appsettings.json"),
            VerboseOutput = configuration.GetValue<bool>("VerboseOutput", false),
            CreateMermaidDiagram = configuration.GetValue<bool>("CreateMermaidDiagram", false),
            TicketFolderPath = configuration["TicketFolderPath"] ?? "./tickets",
            OutputPath = configuration["OUTPUT_PATH"] ?? "output"
        };

        return cfg;
    }
}
