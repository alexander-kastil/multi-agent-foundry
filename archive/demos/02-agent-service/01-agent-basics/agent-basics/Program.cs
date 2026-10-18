using Microsoft.Extensions.Configuration;
using AgentBasics.Models;
using AgentBasics.Services;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = AppConfig.FromConfiguration(configuration);

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Azure AI Agent Service Basics - Demo Menu ===\n");
    Console.WriteLine("1. Basics - Core lifecycle");
    Console.WriteLine("2. EventHandler - Streaming events");
    Console.WriteLine("3. ResponseFormat - Structured output");
    Console.WriteLine("4. InputFile - File input (image)");
    Console.WriteLine("5. InputUrl - URL image input");
    Console.WriteLine("6. InputBase64 - Data URL image input");
    Console.WriteLine("7. Output - Post-processing & external integration");
    Console.WriteLine("8. Tracing - OpenTelemetry tracing with Azure Monitor");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect a demo (1-8): ");

    string? choice = Console.ReadLine();
    Console.Clear();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("=== Demo: Basics - Core lifecycle ===\n");
                var runnerBasics = new AgentRunnerBasics(appConfig);
                await runnerBasics.RunAsync();
                break;

            case "2":
                Console.WriteLine("=== Demo: EventHandler - Streaming events ===\n");
                var runnerEventHandler = new AgentRunnerEventHandler(appConfig);
                await runnerEventHandler.RunAsync();
                break;

            case "3":
                Console.WriteLine("=== Demo: ResponseFormat - Structured output ===\n");
                var runnerResponseFormat = new AgentRunnerResponseFormat(appConfig);
                await runnerResponseFormat.RunAsync();
                break;

            case "4":
                Console.WriteLine("=== Demo: InputFile - File input (image) ===\n");
                var runnerInputFile = new AgentRunnerInputFile(appConfig);
                await runnerInputFile.RunAsync();
                break;

            case "5":
                Console.WriteLine("=== Demo: InputUrl - URL image input ===\n");
                var runnerInputUrl = new AgentRunnerInputUrl(appConfig);
                await runnerInputUrl.RunAsync();
                break;

            case "6":
                Console.WriteLine("=== Demo: InputBase64 - Data URL image input ===\n");
                var runnerInputBase64 = new AgentRunnerInputBase64(appConfig);
                await runnerInputBase64.RunAsync();
                break;

            case "7":
                Console.WriteLine("=== Demo: Output - Post-processing & external integration ===\n");
                var runnerOutput = new AgentRunnerOutput(appConfig);
                await runnerOutput.RunAsync();
                break;

            case "8":
                Console.WriteLine("=== Demo: Tracing - OpenTelemetry tracing with Azure Monitor ===\n");
                var runnerTracing = new AgentRunnerTracing(appConfig);
                await runnerTracing.RunAsync();
                break;

            default:
                Console.WriteLine("Invalid choice. Please select a number from 1-8.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError running demo: {ex.Message}");
    }

    Console.WriteLine("\n\nPress any key to return to the menu...");
    Console.ReadKey();
}

