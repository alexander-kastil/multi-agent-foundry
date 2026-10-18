using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;

namespace AgentBasics.Services;

public sealed class AgentRunnerInputUrl(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");
        Console.WriteLine("Note: This demo sends an image URL to the agent for analysis.");
        Console.WriteLine("The agent should be able to analyze the image if using a vision-capable model (e.g., gpt-4o).\n");

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
            name: "my-agent",
            instructions: "You are helpful agent"
        );
        Console.WriteLine($"Created agent, agent ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        string imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Gfp-wisconsin-madison-the-nature-boardwalk.jpg/2560px-Gfp-wisconsin-madison-the-nature-boardwalk.jpg";
        Console.WriteLine($"Using image URL: {imageUrl}");

        // Create message content blocks with text and image URL
        var contentBlocks = new List<MessageInputContentBlock>
        {
            new MessageInputTextBlock("Can you describe what you see in this image?"),
            new MessageInputImageUriBlock(new MessageImageUriParam(imageUrl))
        };

        PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
            threadId: thread.Id,
            role: MessageRole.User,
            contentBlocks: contentBlocks
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
