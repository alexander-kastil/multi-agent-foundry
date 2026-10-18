using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerMultimodal(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("📄 DEMO: Multimodal Content (Image Analysis)");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\nThis demo would show image analysis capabilities.");
        Console.WriteLine("The Azure AI Agents Persistent API supports multimodal content");
        Console.WriteLine("through file uploads and vision-capable models.");
        
        Console.WriteLine("\nNote: Full implementation requires:");
        Console.WriteLine("  1. Uploading image files to Azure AI");
        Console.WriteLine("  2. Using a vision-capable model (e.g., gpt-4o)");
        Console.WriteLine("  3. Attaching files to messages");
        
        Console.WriteLine("\nExample code pattern:");
        Console.WriteLine("  var file = await client.Files.UploadFileAsync(");
        Console.WriteLine("      filePath: \"invoice.jpg\",");
        Console.WriteLine("      purpose: AgentFilePurpose.Vision);");
        Console.WriteLine();
        Console.WriteLine("  await client.Messages.CreateMessageAsync(");
        Console.WriteLine("      threadId: thread.Id,");
        Console.WriteLine("      role: MessageRole.User,");
        Console.WriteLine("      content: \"Analyze this invoice\",");
        Console.WriteLine("      attachments: new[] { new MessageAttachment(file.Id) });");

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("SIMPLIFIED DEMO - Text-based simulation");
        Console.WriteLine(new string('=', 70));

        var client = new PersistentAgentsClient(
            config.AzureAIProjectEndpoint,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        PersistentAgent agent = await client.Administration.CreateAgentAsync(
            model: config.AzureAIModelDeploymentName,
            name: "MultimodalSimulator",
            instructions: "You are a document analysis assistant. When asked about documents, describe what information you would extract."
        );

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\nAgent created. Ask questions like:");
        Console.WriteLine("  'What information would you extract from an invoice?'");
        Console.WriteLine("  'How would you analyze a product image?'");
        Console.WriteLine("\nType 'quit' to exit\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\n👋 Process completed.");
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
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
