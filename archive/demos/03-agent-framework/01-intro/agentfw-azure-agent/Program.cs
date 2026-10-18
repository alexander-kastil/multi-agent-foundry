using Microsoft.Extensions.Configuration;
using ConnectedAgentsAI.Services;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();

var appConfig = ConnectedAgents.Models.AiAppConfig.FromConfiguration(configuration);

var runner = new AiAgentRunner(appConfig);
await runner.RunAsync();