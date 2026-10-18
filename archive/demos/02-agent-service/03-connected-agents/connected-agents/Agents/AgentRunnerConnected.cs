using Azure.AI.Agents.Persistent;
using Azure.Identity;
using ConnectedAgents.Models;
using ConnectedAgents.Utilities;
using System.Text;

namespace ConnectedAgents.Services;

public sealed class AgentRunnerConnected(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using project endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");

        const string triageAgentInstructions = """
    You are a triage coordinator. For each ticket:
    1. First, determine its priority (High/Medium/Low)
    2. Then, decide which team should handle it (Frontend/Backend/Infrastructure/Marketing)
    3. Finally, estimate the effort required (Small/Medium/Large)

    Provide a clear summary with all three assessments.
    """;

        Console.WriteLine("Initializing PersistentAgentsClient...");
        var agentsClient = new PersistentAgentsClient(
            config.ProjectConnectionString,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );
        Console.WriteLine("PersistentAgentsClient initialized.\n");

        try
        {
            Console.WriteLine("Creating triage agent...");
            PersistentAgent triageAgent = await agentsClient.Administration.CreateAgentAsync(
                model: config.Model,
                name: "triage-agent",
                instructions: triageAgentInstructions
            );
            Console.WriteLine($"Triage agent created: id={triageAgent.Id}\n");

            Console.WriteLine("Creating a new thread for the triage session...");
            PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
            Console.WriteLine($"Thread created: id={thread.Id}\n");

            var prompt = "Users can't reset their password from the mobile app.";
            Console.WriteLine($"Prompt prepared: {prompt}\n");

            Console.WriteLine("Sending user message to thread...");
            PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: prompt
            );
            Console.WriteLine($"Message sent: id={message.Id}, role={message.Role}\n");

            Console.WriteLine("Starting run...");
            ThreadRun run = await agentsClient.Runs.CreateRunAsync(
                thread: thread,
                agent: triageAgent
            );

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(1000);
                run = await agentsClient.Runs.GetRunAsync(thread.Id, run.Id);
            }

            Console.WriteLine($"Run finished: id={run.Id}, status={run.Status}\n");

            if (run.Status == RunStatus.Failed)
            {
                Console.WriteLine($"Run failed: {run.LastError?.Message}");
            }
            else
            {
                Console.WriteLine("Run succeeded. Collecting messages...\n");

                var messages = agentsClient.Messages.GetMessagesAsync(
                    threadId: thread.Id,
                    order: ListSortOrder.Ascending
                );

                var resolutionBuilder = new StringBuilder();

                await foreach (var msg in messages)
                {
                    if (msg.ContentItems.Count > 0)
                    {
                        foreach (var content in msg.ContentItems)
                        {
                            if (content is MessageTextContent textContent)
                            {
                                Console.WriteLine($"Message ({msg.Role}): {textContent.Text.Trim()}\n");
                                if (msg.Role != MessageRole.User)
                                {
                                    resolutionBuilder.Clear();
                                    resolutionBuilder.Append(textContent.Text.Trim());
                                }
                            }
                        }
                    }
                }

                var resolution = resolutionBuilder.ToString();

                if (config.CreateMermaidDiagram)
                {
                    try
                    {
                        Console.WriteLine("Generating Mermaid diagram file...");
                        var ticketsPath = Path.Combine(config.OutputPath, "tickets");
                        var filePath = DiagramHelper.SaveDiagramFile(
                            ticketFolderPath: ticketsPath,
                            ticketPrompt: prompt,
                            resolution: string.IsNullOrWhiteSpace(resolution) ? "Pending" : resolution,
                            tokenUsageIn: 0,
                            tokenUsageOut: 0
                        );
                        Console.WriteLine($"Diagram file saved: {filePath}\n");
                    }
                    catch (Exception genEx)
                    {
                        Console.WriteLine($"Failed to generate diagram file: {genEx.Message}");
                    }
                }
            }

            Console.WriteLine("Cleaning up...");
            await agentsClient.Threads.DeleteThreadAsync(thread.Id);
            Console.WriteLine("Thread deleted.");
            await agentsClient.Administration.DeleteAgentAsync(triageAgent.Id);
            Console.WriteLine("Triage agent deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (config.VerboseOutput)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
