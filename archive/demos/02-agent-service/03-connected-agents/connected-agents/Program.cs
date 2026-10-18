using Microsoft.Extensions.Configuration;
using ConnectedAgents.Models;
using ConnectedAgents.Services;

// Build configuration
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();

// Map configuration to AppConfig
var appConfig = AppConfig.FromConfiguration(configuration);

// Clear the console unless verbose output requested
if (!appConfig.VerboseOutput)
{
    Console.Clear();
}

Console.WriteLine("Starting connected agents triage...");

// Run the agent flow
var runner = new AgentRunnerConnected(appConfig);
await runner.RunAsync();

Console.WriteLine("Finished.");
