using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;

namespace AgentBasics.Services;

public sealed class AgentRunnerBasics(AppConfig config)
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
            name: "basic-agent",
            instructions: "You are helpful agent"
        );
        Console.WriteLine($"Created agent: {agent.Name}, ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
            threadId: thread.Id,
            role: MessageRole.User,
            content: "Hello, tell me a joke"
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
            Console.WriteLine($"Run status: {run.Status}");
        }

        if (run.Status == RunStatus.Failed)
        {
            Console.WriteLine($"Run error: {run.LastError?.Message}");
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
