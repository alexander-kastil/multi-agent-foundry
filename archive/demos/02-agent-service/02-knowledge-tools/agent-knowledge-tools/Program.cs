using Microsoft.Extensions.Configuration;
using AgentKnowledgeTools.Models;
using AgentKnowledgeTools.Agents;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = AppConfig.FromConfiguration(configuration);

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Azure AI Agent Service - Knowledge Tools Demo Menu ===\n");
    Console.WriteLine("1.  Code Interpreter - Data analysis and visualization");
    Console.WriteLine("2.  File Search - Basic agent interaction");
    Console.WriteLine("3.  Function Calling - Custom functions (support tickets)");
    Console.WriteLine("4.  SharePoint - Document search and retrieval");
    Console.WriteLine("5.  REST API Calling - OpenAPI todos management");
    Console.WriteLine("6.  Azure Function - Currency conversion");
    Console.WriteLine("7.  AI Search RAG - Insurance knowledge base");
    Console.WriteLine("8.  MCP - Model Context Protocol integration");
    Console.WriteLine("9.  Bing Grounding - Web search with citations");
    Console.WriteLine("10. Browser Automation - Playwright web automation");
    Console.WriteLine("11. Computer Use - GUI automation");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect a demo (1-11): ");

    string? choice = Console.ReadLine();
    Console.Clear();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("=== Demo: Code Interpreter ===\n");
                var runnerCodeInterpreter = new AgentRunnerCodeInterpreter(appConfig);
                await runnerCodeInterpreter.RunAsync();
                break;

            case "2":
                Console.WriteLine("=== Demo: File Search ===\n");
                var runnerFileSearch = new AgentRunnerFileSearch(appConfig);
                await runnerFileSearch.RunAsync();
                break;

            case "3":
                Console.WriteLine("=== Demo: Function Calling ===\n");
                var runnerFunctionCalling = new AgentRunnerFunctionCalling(appConfig);
                await runnerFunctionCalling.RunAsync();
                break;

            case "4":
                Console.WriteLine("=== Demo: SharePoint ===\n");
                var runnerSharePoint = new AgentRunnerSharePoint(appConfig);
                await runnerSharePoint.RunAsync();
                break;

            case "5":
                Console.WriteLine("=== Demo: REST API Calling ===\n");
                var runnerRestCalling = new AgentRunnerRestCalling(appConfig);
                await runnerRestCalling.RunAsync();
                break;

            case "6":
                Console.WriteLine("=== Demo: Azure Function ===\n");
                var runnerAzFunction = new AgentRunnerAzFunction(appConfig);
                await runnerAzFunction.RunAsync();
                break;

            case "7":
                Console.WriteLine("=== Demo: AI Search RAG ===\n");
                var runnerAiSearchRag = new AgentRunnerAiSearchRag(appConfig);
                await runnerAiSearchRag.RunAsync();
                break;

            case "8":
                Console.WriteLine("=== Demo: MCP ===\n");
                var runnerMcp = new AgentRunnerMcp(appConfig);
                await runnerMcp.RunAsync();
                break;

            case "9":
                Console.WriteLine("=== Demo: Bing Grounding ===\n");
                var runnerBing = new AgentRunnerBingGrounding(appConfig);
                await runnerBing.RunAsync();
                break;

            case "10":
                Console.WriteLine("=== Demo: Browser Automation ===\n");
                var runnerBrowser = new AgentRunnerBrowserAutomation(appConfig);
                await runnerBrowser.RunAsync();
                break;

            case "11":
                Console.WriteLine("=== Demo: Computer Use ===\n");
                var runnerComputerUse = new AgentRunnerComputerUse(appConfig);
                await runnerComputerUse.RunAsync();
                break;

            default:
                Console.WriteLine("Invalid choice. Please select a number from 1-11.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError running demo: {ex.Message}");
        if (appConfig.DetailedLogging)
        {
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    Console.WriteLine("\n\nPress any key to return to the menu...");
    Console.ReadKey();
}
