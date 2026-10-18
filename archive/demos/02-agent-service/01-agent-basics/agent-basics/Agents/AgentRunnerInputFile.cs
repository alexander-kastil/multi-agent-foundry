using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;

namespace AgentBasics.Services;

public sealed class AgentRunnerInputFile(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");
        Console.WriteLine("Note: This demo uploads an image file and attaches it to a message.");
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
            name: "file-search-agent-cs",
            instructions: "You are helpful agent"
        );
        Console.WriteLine($"Created agent, agent ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        string assetFilePath = Path.Combine(AppContext.BaseDirectory, "assets", "soi.jpg");
        PersistentAgentFileInfo imageFile = await agentsClient.Files.UploadFileAsync(
            filePath: assetFilePath,
            purpose: PersistentAgentFilePurpose.Agents
        );
        Console.WriteLine($"Uploaded file, file ID: {imageFile.Id}");

        // Create message content blocks with text and image
        var contentBlocks = new List<MessageInputContentBlock>
        {
            new MessageInputTextBlock("Can you describe what you see in this image?"),
            new MessageInputImageFileBlock(new MessageImageFileParam(imageFile.Id))
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
