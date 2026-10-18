using Microsoft.Extensions.Configuration;
using AgentFwBasics.Models;
using AgentFwBasics.Agents;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = AppConfig.FromConfiguration(configuration);

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Azure Agent Framework Basics - Demo Menu ===\n");
    Console.WriteLine("1.  Create Agent - Create and use Azure AI Foundry agent");
    Console.WriteLine("2.  OpenAI Chat - Direct Azure OpenAI chat (not Agent Service)");
    Console.WriteLine("3.  Chat History - Chat history management");
    Console.WriteLine("4.  Streaming - Response streaming demo");
    Console.WriteLine("5.  Threading - Thread serialization/deserialization");
    Console.WriteLine("6.  Structured Output - Structured output with JSON");
    Console.WriteLine("7.  Middleware - Middleware simulation (timing, logging)");
    Console.WriteLine("8.  Observability - Request/response logging and metrics");
    Console.WriteLine("9.  Multimodal - Multimodal content concepts");
    Console.WriteLine("10. Long Term Memory - AI-powered memory with persistence");
    Console.WriteLine("11. Use Existing Agent - Connect to existing agent");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect a demo (1-11): ");

    string? choice = Console.ReadLine();
    Console.Clear();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("=== Demo 1: Create Agent ===\n");
                var runnerCreateAgent = new AgentRunnerCreateAgent(appConfig);
                await runnerCreateAgent.RunAsync();
                break;

            case "2":
                Console.WriteLine("=== Demo 2: OpenAI Chat ===\n");
                var runnerOpenAIChat = new AgentRunnerOpenAIChat(appConfig);
                await runnerOpenAIChat.RunAsync();
                break;

            case "3":
                Console.WriteLine("=== Demo 3: Chat History ===\n");
                var runnerChatHistory = new AgentRunnerChatHistory(appConfig);
                await runnerChatHistory.RunAsync();
                break;

            case "4":
                Console.WriteLine("=== Demo 4: Streaming ===\n");
                var runnerStreaming = new AgentRunnerStreaming(appConfig);
                await runnerStreaming.RunAsync();
                break;

            case "5":
                Console.WriteLine("=== Demo 5: Threading ===\n");
                var runnerThreading = new AgentRunnerThreading(appConfig);
                await runnerThreading.RunAsync();
                break;

            case "6":
                Console.WriteLine("=== Demo 6: Structured Output ===\n");
                var runnerStructuredOutput = new AgentRunnerStructuredOutput(appConfig);
                await runnerStructuredOutput.RunAsync();
                break;

            case "7":
                Console.WriteLine("=== Demo 7: Middleware ===\n");
                var runnerMiddleware = new AgentRunnerMiddleware(appConfig);
                await runnerMiddleware.RunAsync();
                break;

            case "8":
                Console.WriteLine("=== Demo 8: Observability ===\n");
                var runnerObservability = new AgentRunnerObservability(appConfig);
                await runnerObservability.RunAsync();
                break;

            case "9":
                Console.WriteLine("=== Demo 9: Multimodal ===\n");
                var runnerMultimodal = new AgentRunnerMultimodal(appConfig);
                await runnerMultimodal.RunAsync();
                break;

            case "10":
                Console.WriteLine("=== Demo 10: Long Term Memory ===\n");
                var runnerLongTermMemory = new AgentRunnerLongTermMemory(appConfig);
                await runnerLongTermMemory.RunAsync();
                break;

            case "11":
                Console.WriteLine("=== Demo 11: Use Existing Agent ===\n");
                var runnerUseExistingAgent = new AgentRunnerUseExistingAgent(appConfig);
                await runnerUseExistingAgent.RunAsync();
                break;

            default:
                Console.WriteLine("Invalid choice. Please select a number from 1-11.");
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
