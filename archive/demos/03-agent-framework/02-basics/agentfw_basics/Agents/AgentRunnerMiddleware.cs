using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;
using System.Diagnostics;

namespace AgentFwBasics.Agents;

public class AgentRunnerMiddleware(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 75));
        Console.WriteLine("MIDDLEWARE DEMO - Timing and Logging Simulation");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("\nThis demo simulates middleware concepts:");
        Console.WriteLine("  1. TIMING - Tracks execution time");
        Console.WriteLine("  2. LOGGING - Logs all requests and responses");
        Console.WriteLine("\nNote: C# Persistent Agents don't have middleware like Python Agent Framework,");
        Console.WriteLine("but we can simulate the concept with timing and logging wrappers.");
        Console.WriteLine(new string('=', 75));

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: config.AzureAIModelDeploymentName,
            name: "MiddlewareDemo",
            instructions: "You are a helpful assistant. Be friendly and concise."
        );

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\nAgent created with simulated middleware.\n");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("SUGGESTED TEST PROMPTS:");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("\nPROMPT 1: 'tell me a joke'");
        Console.WriteLine("   → Triggers: Timing + Logging");
        Console.WriteLine("\nPROMPT 2: 'what is 15 * 8?'");
        Console.WriteLine("   → Triggers: Timing + Logging");
        Console.WriteLine("\nType 'quit' to exit");
        Console.WriteLine(new string('=', 75) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\nDemo ended. Thanks for testing the middleware simulation.");
                break;
            }

            Console.WriteLine("\n" + new string('-', 75));
            Console.WriteLine("PROCESSING YOUR REQUEST...");
            Console.WriteLine(new string('-', 75));

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"\n[TIMING] Started at {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"[LOGGING] User message: {userInput}");

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

            Console.Write("\nAgent: ");

            var streamingRun = client.Runs.CreateRunStreamingAsync(
                threadId: thread.Id,
                agentId: agent.Id
            );

            var response = "";
            await foreach (var update in streamingRun)
            {
                if (update is MessageContentUpdate contentUpdate)
                {
                    Console.Write(contentUpdate.Text);
                    response += contentUpdate.Text;
                }
            }

            stopwatch.Stop();

            Console.WriteLine("\n");
            Console.WriteLine($"[LOGGING] Agent response: {response}");
            Console.WriteLine($"[TIMING] Completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine(new string('-', 75));
            Console.WriteLine("Request completed.\n");
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
