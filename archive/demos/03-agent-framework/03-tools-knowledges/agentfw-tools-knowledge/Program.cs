using Microsoft.Extensions.Configuration;
using AgentFwToolsKnowledge.Models;
using AgentFwToolsKnowledge.Agents;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = AppConfig.FromConfiguration(configuration);

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Azure Agent Framework - Tools & Knowledge Demo Menu ===\n");
    Console.WriteLine("1. Built-in Tools - Code Interpreter & Web Search (educational)");
    Console.WriteLine("2. File Search - Search documents in vector store");
    Console.WriteLine("3. Calculator - Custom function tool for math calculations");
    Console.WriteLine("4. Multiple Tools - Weather, calculator, and time zone tools");
    Console.WriteLine("5. REST API Tool - DummyJSON Todos API integration");
    Console.WriteLine("6. Human-in-the-Loop - Approval system for dangerous operations");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect a demo (1-6): ");

    string? choice = Console.ReadLine();
    Console.Clear();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("=== Demo 1: Built-in Tools ===\n");
                var runnerBuiltinTools = new AgentRunnerBuiltinTools(appConfig);
                await runnerBuiltinTools.RunAsync();
                break;

            case "2":
                Console.WriteLine("=== Demo 2: File Search ===\n");
                var runnerFileSearch = new AgentRunnerFileSearch(appConfig);
                await runnerFileSearch.RunAsync();
                break;

            case "3":
                Console.WriteLine("=== Demo 3: Calculator ===\n");
                var runnerCalculator = new AgentRunnerCalculator(appConfig);
                await runnerCalculator.RunAsync();
                break;

            case "4":
                Console.WriteLine("=== Demo 4: Multiple Tools ===\n");
                var runnerMultipleTools = new AgentRunnerMultipleTools(appConfig);
                await runnerMultipleTools.RunAsync();
                break;

            case "5":
                Console.WriteLine("=== Demo 5: REST API Tool ===\n");
                var runnerRestApiTool = new AgentRunnerRestApiTool(appConfig);
                await runnerRestApiTool.RunAsync();
                break;

            case "6":
                Console.WriteLine("=== Demo 6: Human-in-the-Loop ===\n");
                var runnerHumanInTheLoop = new AgentRunnerHumanInTheLoop(appConfig);
                await runnerHumanInTheLoop.RunAsync();
                break;

            default:
                Console.WriteLine("Invalid choice. Please select a number from 1-6.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError running demo: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }

    Console.WriteLine("\n\nPress any key to return to the menu...");
    Console.ReadKey();
}

