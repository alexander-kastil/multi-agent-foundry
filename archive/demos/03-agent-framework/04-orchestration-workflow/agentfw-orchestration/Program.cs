using Microsoft.Extensions.Configuration;
using AFWOrchestration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = OrchestrationAppConfig.FromConfiguration(configuration);

var runner = new OrchestrationRunner(appConfig);
await runner.RunAsync();
