using System.Text.Json;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerLongTermMemory(AppConfig config)
{
    private class UserProfile
    {
        public Dictionary<string, string> Facts { get; set; } = new();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("LONG-TERM MEMORY DEMO with FILE PERSISTENCE");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\nConcept: Agent remembers facts about you across sessions!");
        
        var memoryFile = Path.Combine(config.OutputPath, "user_memory.json");
        Directory.CreateDirectory(config.OutputPath);

        Console.WriteLine($"Memory File: {memoryFile}");
        Console.WriteLine(new string('=', 70));

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        var profile = new UserProfile();

        if (File.Exists(memoryFile))
        {
            var json = await File.ReadAllTextAsync(memoryFile);
            var loaded = JsonSerializer.Deserialize<UserProfile>(json);
            if (loaded != null)
            {
                profile = loaded;
                Console.WriteLine($"\n📂 [LOADED MEMORY] from {memoryFile}");
                if (profile.Facts.Count > 0)
                {
                    Console.WriteLine($"   🧠 Restored profile: {string.Join(", ", profile.Facts.Select(kv => $"{kv.Key}={kv.Value}"))}");
                }
            }
        }
        else
        {
            Console.WriteLine($"\n[NEW MEMORY] No existing memory file found");
        }

        var profileInstructions = profile.Facts.Count > 0
            ? $"\n\nYou know these facts about the user:\n{string.Join("\n", profile.Facts.Select(kv => $"- {kv.Key}: {kv.Value}"))}\n\nReference these naturally and be enthusiastic when recognizing the user!"
            : "";

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: config.AzureAIModelDeploymentName,
            name: "MemoryAgent",
            instructions: $"You are a friendly assistant with long-term memory. Be conversational and warm.{profileInstructions}"
        );

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\nAgent created with memory capabilities\n");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("COMMANDS:");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("  • Chat naturally - Tell the agent about yourself");
        Console.WriteLine("  • 'remember <key>=<value>' - Manually save a fact");
        Console.WriteLine("  • 'profile' - Show what the agent remembers");
        Console.WriteLine("  • 'quit' - Exit");
        Console.WriteLine(new string('=', 70) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() == "quit")
            {
                Console.WriteLine("\nDemo ended!");
                if (profile.Facts.Count > 0)
                {
                    Console.WriteLine("\n📊 Saved Profile:");
                    foreach (var kv in profile.Facts)
                    {
                        Console.WriteLine($"   • {kv.Key}: {kv.Value}");
                    }
                }
                break;
            }

            if (userInput.Trim().ToLower() == "profile")
            {
                Console.WriteLine("\nSAVED PROFILE:");
                if (profile.Facts.Count > 0)
                {
                    foreach (var kv in profile.Facts)
                    {
                        Console.WriteLine($"   • {kv.Key}: {kv.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("   (No facts saved yet)");
                }
                Console.WriteLine();
                continue;
            }

            if (userInput.Trim().ToLower().StartsWith("remember "))
            {
                var parts = userInput.Substring(9).Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    profile.Facts[key] = value;
                    Console.WriteLine($"   💾 [LEARNED] {key} = {value}");
                    
                    await File.WriteAllTextAsync(memoryFile, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"   💾 [SAVED TO FILE] {memoryFile}\n");
                }
                continue;
            }

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

            Console.Write($"Agent: ");

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

            Console.WriteLine("\n");
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
