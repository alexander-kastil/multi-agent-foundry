using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Azure.Storage.Blobs;
using AgentBasics.Models;
using QRCoder;

namespace AgentBasics.Services;

public sealed class AgentRunnerOutput(AppConfig config)
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
            content: "Hello"
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

        var messages = agentsClient.Messages.GetMessagesAsync(
            threadId: thread.Id,
            order: ListSortOrder.Ascending
        );

        string agentResponse = string.Empty;
        await foreach (var msg in messages)
        {
            if (msg.ContentItems.Count > 0)
            {
                foreach (var content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        Console.WriteLine($"{msg.Role}: {textContent.Text.Trim()}");
                        if (msg.Role != MessageRole.User)
                        {
                            agentResponse = textContent.Text.Trim();
                        }
                    }
                }
            }
        }

        await agentsClient.Administration.DeleteAgentAsync(agent.Id);
        Console.WriteLine("Deleted agent");

        Console.Write("\nWhat do you want to encode? Press Enter for default: https://www.integrations.at\n");
        string? userInput = Console.ReadLine();
        string qrContent = !string.IsNullOrWhiteSpace(userInput) ? userInput : "https://www.integrations.at";

        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
        PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);

        using MemoryStream memoryStream = new MemoryStream(qrCodeBytes);

        if (string.IsNullOrEmpty(config.StorageConnectionString))
        {
            Console.WriteLine("\nStorageConnectionString not configured. Skipping blob upload.");
            return;
        }

        string dateStr = DateTime.Now.ToString("yyyyMMdd");
        string blobName = $"qr{dateStr}.png";

        BlobServiceClient blobServiceClient = new BlobServiceClient(config.StorageConnectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(config.StorageContainerName ?? "agent-basics");

        await containerClient.CreateIfNotExistsAsync();
        Console.WriteLine($"Container ready: {containerClient.Name}");

        BlobClient blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(memoryStream, overwrite: true);

        string downloadUrl = blobClient.Uri.ToString();
        Console.WriteLine($"QR Code uploaded. Download URL: {downloadUrl}");
    }
}
