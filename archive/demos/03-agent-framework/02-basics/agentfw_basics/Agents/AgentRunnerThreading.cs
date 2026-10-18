using System.Text.Json;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerThreading(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("AUTO-SERIALIZATION DEMO: Thread Save/Restore");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("Demo Guide:");
        Console.WriteLine("  1. Type a message (e.g. 'I am Alex')");
        Console.WriteLine("  2. Agent responds using current thread context");
        Console.WriteLine("  3. Thread ID is saved");
        Console.WriteLine("  4. On next run, you can restore the thread");
        Console.WriteLine("  5. Type 'quit' to exit the demo");
        Console.WriteLine(new string('=', 70));

        var threadFile = Path.Combine(config.OutputPath, "thread_history.json");
        Directory.CreateDirectory(config.OutputPath);

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
            name: "MemoryBot",
            instructions: "You are a helpful assistant. Remember everything the user tells you and refer back to it."
        );

        Console.WriteLine("\nAgent created");

        PersistentAgentThread thread;
        int messageCount = 0;

        Console.WriteLine("Checking for existing thread...");
        if (File.Exists(threadFile))
        {
            Console.Write($"   Found {threadFile}. Load previous conversation? (y/n): ");
            var choice = Console.ReadLine()?.Trim().ToLower();

            if (choice == "y" || choice == "yes")
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    await File.ReadAllTextAsync(threadFile)
                );

                if (data != null && data.TryGetValue("thread_id", out var threadIdObj))
                {
                    var threadId = threadIdObj.ToString();
                    if (!string.IsNullOrEmpty(threadId))
                    {
                        thread = await client.Threads.GetThreadAsync(threadId);
                        if (data.TryGetValue("message_count", out var countObj) && 
                            int.TryParse(countObj.ToString(), out var count))
                        {
                            messageCount = count;
                        }
                        Console.WriteLine($"   Restored thread with {messageCount} messages.");
                    }
                    else
                    {
                        thread = await client.Threads.CreateThreadAsync();
                        Console.WriteLine("   Creating new thread...");
                    }
                }
                else
                {
                    thread = await client.Threads.CreateThreadAsync();
                    Console.WriteLine("   Creating new thread...");
                }
            }
            else
            {
                thread = await client.Threads.CreateThreadAsync();
                Console.WriteLine("   Creating new thread...");
            }
        }
        else
        {
            thread = await client.Threads.CreateThreadAsync();
            Console.WriteLine("   Creating new thread...");
        }

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("Interactive Chat with Auto-Serialization");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("After each message:");
        Console.WriteLine("   1. Agent responds");
        Console.WriteLine("   2. Thread ID automatically saves");
        Console.WriteLine("\nType 'quit' to exit");
        Console.WriteLine(new string('=', 70) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine($"\nDemo completed. Total messages: {messageCount}");
                break;
            }

            messageCount++;
            Console.WriteLine($"\n[Message #{messageCount}]");

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

            Console.Write("Agent: ");

            var streamingRun = client.Runs.CreateRunStreamingAsync(
                threadId: thread.Id,
                agentId: agent.Id
            );

            await foreach (var update in streamingRun)
            {
                if (update is MessageContentUpdate contentUpdate)
                {
                    Console.Write(contentUpdate.Text);
                }
            }

            Console.WriteLine();

            Console.WriteLine($"\n[Auto-Saving thread state to {threadFile}...]");
            var saveData = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["message_count"] = messageCount,
                ["thread_id"] = thread.Id
            };

            await File.WriteAllTextAsync(threadFile, JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("   Thread ID saved to disk");
            Console.WriteLine("   Next message will continue this thread\n");
            Console.WriteLine(new string('-', 70) + "\n");
        }

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO COMPLETE");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("What you saw:");
        Console.WriteLine("   • Thread ID saved to JSON file after each message");
        Console.WriteLine("   • Thread can be restored on next run");
        Console.WriteLine("   • Agent maintained conversation history");
        Console.WriteLine($"\nCheck the file: {threadFile}");
        Console.WriteLine(new string('=', 70) + "\n");

        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
