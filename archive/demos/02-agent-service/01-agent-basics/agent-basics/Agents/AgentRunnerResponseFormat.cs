using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;

namespace AgentBasics.Services;

public sealed class AgentRunnerResponseFormat(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");

        var agentsClient = new PersistentAgentsClient(
            config.ProjectConnectionString,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        PersistentAgent agent = await agentsClient.Administration.CreateAgentAsync(
            model: config.Model,
            name: "response-agent",
            instructions: "You are helpful agent. You will respond with a JSON object."
        );
        Console.WriteLine($"Created agent, agent ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
            threadId: thread.Id,
            role: MessageRole.User,
            content: "Hello, give me a list of planets in our solar system, and their mass in kilograms."
        );
        Console.WriteLine($"Created message, message ID: {message.Id}");

        ThreadRun run = await agentsClient.Runs.CreateRunAsync(
            thread: thread,
            agent: agent
        );

        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
        {
            await Task.Delay(1000);
            run = await agentsClient.Runs.GetRunAsync(thread.Id, run.Id);
        }

        if (run.Status != RunStatus.Completed)
        {
            Console.WriteLine($"The run did not succeed: {run.Status}.");
        }

        await agentsClient.Administration.DeleteAgentAsync(agent.Id);
        Console.WriteLine("Deleted agent");

        var messages = agentsClient.Messages.GetMessagesAsync(
            threadId: thread.Id,
            order: ListSortOrder.Ascending
        );

        await foreach (var msg in messages)
        {
            if (msg.ContentItems.Count > 0)
            {
                foreach (var content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        Console.WriteLine($"{msg.Role}: {textContent.Text.Trim()}");
                    }
                }
            }
        }
    }
}
