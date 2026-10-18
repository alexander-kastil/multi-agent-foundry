using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwToolsKnowledge.Models;

namespace AgentFwToolsKnowledge.Agents;

public class AgentRunnerFileSearch(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("🔍 DEMO: File Search Tool");
        Console.WriteLine(new string('=', 70));

        // Check if vector store ID is valid
        if (string.IsNullOrWhiteSpace(config.VectorStoreId))
        {
            Console.WriteLine("\n❌ ERROR: Invalid or missing VectorStoreId");
            Console.WriteLine("\n⚠️  Please set a valid VectorStoreId in appsettings.json");
            Console.WriteLine("   1. Go to Azure AI Foundry portal");
            Console.WriteLine("   2. Create a Vector Store");
            Console.WriteLine("   3. Upload documents (PDF, TXT, DOCX)");
            Console.WriteLine("   4. Copy the Vector Store ID");
            Console.WriteLine("   5. Add VectorStoreId to appsettings.json");
            Console.WriteLine($"\n   Current value: '{config.VectorStoreId}'\n");
            return;
        }

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        Console.WriteLine("\nCreating agent with File Search tool...");

        // Create file search tool resource
        var fileSearchTool = new FileSearchToolDefinition();
        var fileSearchResource = new FileSearchToolResource();
        fileSearchResource.VectorStoreIds.Add(config.VectorStoreId);

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: config.AzureAIModelDeploymentName,
            name: "File Search Agent",
            instructions: "You are a document search assistant. Use the file search tool to find information in uploaded documents.",
            tools: [fileSearchTool],
            toolResources: new ToolResources
            {
                FileSearch = fileSearchResource
            }
        );

        Console.WriteLine($"Agent created successfully!");
        Console.WriteLine($"   Agent ID: {agent.Id}");

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\n✅ Agent created with File Search Tool");
        Console.WriteLine("💡 TIP: Ask questions about documents in your vector store");
        Console.WriteLine($"📝 Example: Tell me about the documents in the store");

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("💬 Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\n👋 Goodbye!");
                break;
            }

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

            ThreadRun run = await client.Runs.CreateRunAsync(
                thread: thread,
                agent: agent
            );

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(1000);
                run = await client.Runs.GetRunAsync(thread.Id, run.Id);
            }

            Console.Write("Agent: ");

            var messages = client.Messages.GetMessagesAsync(
                threadId: thread.Id,
                order: ListSortOrder.Descending
            );

            await foreach (var message in messages)
            {
                if (message.Role == MessageRole.Agent)
                {
                    foreach (var content in message.ContentItems)
                    {
                        if (content is MessageTextContent textContent)
                        {
                            Console.Write(textContent.Text.Trim());
                        }
                    }
                    break;
                }
            }

            Console.WriteLine("\n");
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
        Console.WriteLine("👋 See you again soon.");
    }
}
