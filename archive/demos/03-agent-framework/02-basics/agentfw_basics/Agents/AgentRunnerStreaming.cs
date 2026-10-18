using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerStreaming(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO: Response Streaming with Agent Framework");
        Console.WriteLine(new string('=', 70));

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        Console.WriteLine("\nCreating new agent in Azure AI Foundry...");

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: config.AzureAIModelDeploymentName,
            name: "Streaming Demo Agent",
            instructions: "You are a helpful AI assistant that provides detailed, informative responses. Be thorough and engaging in your explanations.",
            description: "Agent demonstrating streaming capabilities"
        );

        Console.WriteLine($"Agent created successfully!");
        Console.WriteLine($"   Agent ID: {agent.Id}");
        Console.WriteLine($"   Name: {agent.Name}");

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("Interactive Streaming Chat");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\nTIP: Responses will stream in real-time, token by token");
        Console.WriteLine("TIP: Try asking for longer responses to see streaming in action");
        Console.WriteLine("Examples:");
        Console.WriteLine("   - 'Explain how neural networks work'");
        Console.WriteLine("   - 'Tell me a creative story about a robot'");
        Console.WriteLine("   - 'Describe the process of photosynthesis'");
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("Type 'quit' to exit\n");

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

            Console.Write("\nAgent: ");

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
