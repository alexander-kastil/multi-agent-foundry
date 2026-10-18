using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;

namespace AgentBasics.Services;

public sealed class AgentRunnerInputBase64(AppConfig config)
{
    private static string ImageToBase64(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"File not found at: {imagePath}");
        }

        byte[] fileData = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(fileData);
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");
        Console.WriteLine("Note: This demo sends a Base64-encoded image as a data URL to the agent.");
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
            name: "base64-agent-cs",
            instructions: "You are helpful agent"
        );
        Console.WriteLine($"Created agent, agent ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        string assetFilePath = Path.Combine(AppContext.BaseDirectory, "assets", "soi.jpg");
        string imageBase64 = ImageToBase64(assetFilePath);
        Console.WriteLine($"Converted image to Base64 (length: {imageBase64.Length} characters)");

        // Create data URL with base64-encoded image
        string dataUrl = $"data:image/jpeg;base64,{imageBase64}";

        // Create message content blocks with text and base64 image
        var contentBlocks = new List<MessageInputContentBlock>
        {
            new MessageInputTextBlock("Can you describe what you see in this image?"),
            new MessageInputImageUriBlock(new MessageImageUriParam(dataUrl))
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
