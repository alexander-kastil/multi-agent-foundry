using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentBasics.Models;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace AgentBasics.Services;

public sealed class AgentRunnerTracing(AppConfig config)
{
    private static readonly ActivitySource ActivitySource = new("AgentBasics.Tracing");

    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");

        // Get Application Insights connection string from environment or config
        string? connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Warning: APPLICATIONINSIGHTS_CONNECTION_STRING not set. Tracing will not be exported to Azure Monitor.");
            Console.WriteLine("Set the environment variable to enable tracing to Application Insights.\n");
        }
        else
        {
            Console.WriteLine("Application Insights configured for tracing\n");
        }

        // Configure OpenTelemetry with Azure Monitor
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentBasics"))
            .AddSource(ActivitySource.Name)
            .AddAzureMonitorTraceExporter(options =>
            {
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.ConnectionString = connectionString;
                }
            })
            .Build();

        var agentsClient = new PersistentAgentsClient(
            config.ProjectConnectionString,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        // Create agent with custom span
        using (var createAgentActivity = ActivitySource.StartActivity("create_agent"))
        {
            PersistentAgent agent = await agentsClient.Administration.CreateAgentAsync(
                model: config.Model,
                name: "tracing-agent-cs",
                instructions: "You are helpful agent"
            );
            Console.WriteLine($"Created agent, agent ID: {agent.Id}");

            // Add custom attributes to span
            createAgentActivity?.SetTag("agent.id", agent.Id);
            createAgentActivity?.SetTag("agent.model", config.Model);

            // Create thread with custom span
            PersistentAgentThread thread;
            using (var createThreadActivity = ActivitySource.StartActivity("create_thread"))
            {
                thread = await agentsClient.Threads.CreateThreadAsync();
                Console.WriteLine($"Created thread, thread ID: {thread.Id}");

                createThreadActivity?.SetTag("thread.id", thread.Id);
            }

            // Upload file with custom span
            using (var uploadFileActivity = ActivitySource.StartActivity("upload_file"))
            {
                string assetFilePath = Path.Combine(AppContext.BaseDirectory, "assets", "soi.jpg");
                PersistentAgentFileInfo imageFile = await agentsClient.Files.UploadFileAsync(
                    filePath: assetFilePath,
                    purpose: PersistentAgentFilePurpose.Agents
                );
                Console.WriteLine($"Uploaded file, file ID: {imageFile.Id}");

                uploadFileActivity?.SetTag("file.id", imageFile.Id);
                uploadFileActivity?.SetTag("file.path", assetFilePath);

                // Create message with custom span
                using (var createMessageActivity = ActivitySource.StartActivity("create_message"))
                {
                    string inputMessage = $"Hello, I've uploaded a file with ID {imageFile.Id}. Can you tell me about images?";

                    PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
                        threadId: thread.Id,
                        role: MessageRole.User,
                        content: inputMessage
                    );
                    Console.WriteLine($"Created message, message ID: {message.Id}");

                    createMessageActivity?.SetTag("message.id", message.Id);
                    createMessageActivity?.SetTag("message.content", inputMessage);

                    // Run agent with custom span
                    using (var runAgentActivity = ActivitySource.StartActivity("run_agent"))
                    {
                        ThreadRun run = await agentsClient.Runs.CreateRunAsync(
                            thread: thread,
                            agent: agent
                        );

                        runAgentActivity?.SetTag("run.id", run.Id);

                        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
                        {
                            await Task.Delay(1000);
                            run = await agentsClient.Runs.GetRunAsync(thread.Id, run.Id);
                        }

                        runAgentActivity?.SetTag("run.status", run.Status.ToString());

                        if (run.Status != RunStatus.Completed)
                        {
                            Console.WriteLine($"The run did not succeed: {run.Status}.");
                            runAgentActivity?.SetTag("run.success", false);
                        }
                        else
                        {
                            runAgentActivity?.SetTag("run.success", true);
                        }
                    }
                }
            }

            // Clean up
            await agentsClient.Administration.DeleteAgentAsync(agent.Id);
            Console.WriteLine("Deleted agent");

            // Retrieve and display messages with custom span
            using (var retrieveMessagesActivity = ActivitySource.StartActivity("retrieve_messages"))
            {
                var messages = agentsClient.Messages.GetMessagesAsync(
                    threadId: thread.Id,
                    order: ListSortOrder.Ascending
                );

                int messageCount = 0;
                await foreach (var msg in messages)
                {
                    messageCount++;
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

                retrieveMessagesActivity?.SetTag("messages.count", messageCount);
            }
        }

        Console.WriteLine("\nTracing complete. View traces in Azure AI Foundry portal under Tracing section.");
    }
}
