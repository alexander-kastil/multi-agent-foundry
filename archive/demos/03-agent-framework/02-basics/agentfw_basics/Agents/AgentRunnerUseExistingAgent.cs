using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerUseExistingAgent(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO: Connect to Existing Azure AI Foundry Agent");
        Console.WriteLine(new string('=', 70));

        if (string.IsNullOrWhiteSpace(config.AzureAIAgentId))
        {
            Console.WriteLine("\n❌ AzureAIAgentId is not configured.");
            Console.WriteLine("Please set AzureAIAgentId in appsettings.json");
            return;
        }

        Console.WriteLine($"\nConnecting to agent: {config.AzureAIAgentId}");

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        PersistentAgent agent;
        try
        {
            agent = await client.Administration.GetAgentAsync(config.AzureAIAgentId);
            Console.WriteLine("Connected successfully!");
            Console.WriteLine($"   Agent Name: {agent.Name}");
            Console.WriteLine($"   Model: {agent.Model}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Failed to connect to agent: {ex.Message}");
            return;
        }

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\nGoodbye!");
                break;
            }

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

            Console.WriteLine("\n");
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
    }
}
