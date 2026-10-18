using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerCreateAgent(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO: Create Azure AI Foundry Agent (Interactive)");
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
            name: "First AFW Agent",
            instructions: "You are a helpful AI assistant. Be concise and friendly.",
            description: "Created by AgentFwBasics demo"
        );

        Console.WriteLine($"Agent created successfully!");
        Console.WriteLine($"   Agent ID: {agent.Id}");

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
                            Console.WriteLine(textContent.Text.Trim());
                        }
                    }
                    break;
                }
            }

            Console.WriteLine();
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
