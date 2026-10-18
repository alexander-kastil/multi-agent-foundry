using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AFWOrchestration;

public class OrchestrationRunner(OrchestrationAppConfig config)
{

    public async Task RunAsync()
    {
        // Create the Persistent Agents Client
        PersistentAgentsClient client = new PersistentAgentsClient(
            config.ProjectConnectionString,
            new AzureCliCredential());

        // Create the incident manager agent with log file reading capability
        PersistentAgent incidentAgent = await client.Administration.CreateAgentAsync(
            model: config.Model,
            name: IncidentManager.Name,
            instructions: IncidentManager.Instructions,
            tools: new List<ToolDefinition> { LogFilePlugin.GetToolDefinition() }
        );

        // Set outcome directory for plugins
        var outcomeDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.OutcomeDirectory);
        DevopsPlugin.OutcomeDirectory = outcomeDirectory;
        LogFilePlugin.OutcomeDirectory = outcomeDirectory;

        // Create the devops agent with devops operation capabilities
        PersistentAgent devOpsAgent = await client.Administration.CreateAgentAsync(
            model: config.Model,
            name: DevOpsAssistant.Name,
            instructions: DevOpsAssistant.Instructions,
            tools: DevopsPlugin.GetToolDefinitions()
        );

        // Get all log files in the "logs" directory
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.LogDirectory);
        foreach (var filePath in Directory.GetFiles(logDirectory))
        {
            var fileName = Path.GetFileName(filePath);
            Console.WriteLine($"\nProcessing log file: {fileName}\n");

            // Write a yellow summary of log severities before analysis
            LogFilePlugin.PrintLogSummary(filePath);

            // Create a new thread for each log file
            PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

            try
            {
                int maxIterations = 5;
                int iteration = 0;
                bool resolved = false;

                while (iteration < maxIterations && !resolved)
                {
                    iteration++;

                    // Create the prompt for this iteration
                    string prompt = iteration == 1
                        ? $"Analyze this log file and recommend corrective action: {filePath}"
                        : $"Re-read the log file {filePath} to verify if the previous issue has been resolved. If the error is gone, respond with 'No action needed'. If the error still exists or a new error appeared, recommend the appropriate corrective action.";

                    // Create user message
                    await client.Messages.CreateMessageAsync(
                        threadId: thread.Id,
                        role: MessageRole.User,
                        content: prompt
                    );

                    // Run the incident manager
                    ThreadRun incidentRun = await client.Runs.CreateRunAsync(
                        thread: thread,
                        agent: incidentAgent
                    );

                    // Process the incident manager run
                    incidentRun = await ProcessRunWithToolCalls(client, thread.Id, incidentRun);

                    if (incidentRun.Status == RunStatus.Failed)
                    {
                        Console.WriteLine($"Incident manager run failed: {incidentRun.LastError?.Message}");
                        if (incidentRun.LastError?.Code == "rate_limit_exceeded")
                        {
                            Console.WriteLine("Waiting for rate limit...");
                            await Task.Delay(60000);
                            continue;
                        }
                        break;
                    }

                    // Get the incident manager recommendation
                    var messages = client.Messages.GetMessagesAsync(
                        threadId: thread.Id,
                        order: ListSortOrder.Descending);

                    PersistentThreadMessage? lastMessage = null;
                    await foreach (var msg in messages)
                    {
                        if (msg.Role == MessageRole.Agent)
                        {
                            lastMessage = msg;
                            break;
                        }
                    }

                    if (lastMessage?.ContentItems.FirstOrDefault() is MessageTextContent textContent)
                    {
                        string recommendation = textContent.Text;
                        Console.WriteLine($"Iteration {iteration} - Incident Manager: {recommendation}\n");

                        // Check if resolved or escalated
                        if (recommendation.Contains("No action needed", StringComparison.OrdinalIgnoreCase))
                        {
                            resolved = true;
                            LogFilePlugin.PrintOutcome($"Issue in {fileName} resolved.\n");
                            break;
                        }

                        if (recommendation.Contains("Escalate issue", StringComparison.OrdinalIgnoreCase))
                        {
                            resolved = true;
                            LogFilePlugin.PrintOutcome($"Issue in {fileName} escalated to higher support tier.\n");
                            // Still execute the escalation action before breaking
                        }

                        // Create a message for the devops agent
                        await client.Messages.CreateMessageAsync(
                            threadId: thread.Id,
                            role: MessageRole.User,
                            content: recommendation
                        );

                        // Run the devops agent
                        ThreadRun devopsRun = await client.Runs.CreateRunAsync(
                            thread: thread,
                            agent: devOpsAgent
                        );

                        // Process the devops agent run
                        devopsRun = await ProcessRunWithToolCalls(client, thread.Id, devopsRun);

                        if (devopsRun.Status == RunStatus.Failed)
                        {
                            Console.WriteLine($"DevOps agent run failed: {devopsRun.LastError?.Message}");
                            break;
                        }

                        // Get the devops agent response
                        var devopsMessages = client.Messages.GetMessagesAsync(
                            threadId: thread.Id,
                            order: ListSortOrder.Descending);

                        await foreach (var msg in devopsMessages)
                        {
                            if (msg.Role == MessageRole.Agent)
                            {
                                if (msg.ContentItems.FirstOrDefault() is MessageTextContent devopsText)
                                {
                                    Console.WriteLine($"Iteration {iteration} - DevOps Assistant: {devopsText.Text}\n");
                                }
                                break;
                            }
                        }
                    }

                    await Task.Delay(1000); // Wait to reduce TPM
                }

                // Write final outcome
                var outcomeMessage = resolved
                    ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] RESOLVED: Issue in {fileName} was successfully resolved after {iteration} iteration(s)."
                    : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INCOMPLETE: Issue in {fileName} could not be fully resolved after {maxIterations} iterations.";
                LogFilePlugin.WriteOutcome(filePath, outcomeMessage + "\n");
                LogFilePlugin.PrintOutcome(outcomeMessage + "\n");

                // Clean up thread
                await client.Threads.DeleteThreadAsync(thread.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing {fileName}: {e.Message}");
                if (e.Message.Contains("Rate limit is exceeded"))
                {
                    Console.WriteLine("Waiting...");
                    await Task.Delay(60000);
                    continue;
                }
            }
        }

        // Clean up agents
        await client.Administration.DeleteAgentAsync(incidentAgent.Id);
        await client.Administration.DeleteAgentAsync(devOpsAgent.Id);
    }

    private static async Task<ThreadRun> ProcessRunWithToolCalls(PersistentAgentsClient client, string threadId, ThreadRun run)
    {
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
        {
            await Task.Delay(1000);
            run = await client.Runs.GetRunAsync(threadId, run.Id);

            if (run.Status == RunStatus.RequiresAction && run.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
            {
                var toolOutputs = new List<ToolOutput>();

                foreach (var toolCall in submitToolOutputsAction.ToolCalls)
                {
                    if (toolCall is RequiredFunctionToolCall functionToolCall)
                    {
                        string result = ToolCallHandler.HandleToolCall(
                            functionToolCall.Name,
                            functionToolCall.Arguments
                        );
                        toolOutputs.Add(new ToolOutput(toolCall.Id, result));
                    }
                }

                if (toolOutputs.Any())
                {
                    run = await client.Runs.SubmitToolOutputsToRunAsync(run, toolOutputs);
                }
            }
        }

        return run;
    }
}
