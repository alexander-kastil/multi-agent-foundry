using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerObservability(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 75));
        Console.WriteLine("OBSERVABILITY DEMO - Request/Response Logging");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("\nThis demo shows basic observability through logging:");
        Console.WriteLine("  ✅ Request timestamps");
        Console.WriteLine("  ✅ Response times");
        Console.WriteLine("  ✅ Token usage tracking");
        Console.WriteLine("  ✅ Message content logging");
        Console.WriteLine("\nNote: Full OpenTelemetry integration would require additional packages");
        Console.WriteLine("like OpenTelemetry.Exporter.Console and custom span processors.");
        Console.WriteLine(new string('=', 75));

        var outputPath = Path.Combine(config.OutputPath, "observability_log.txt");
        Directory.CreateDirectory(config.OutputPath);

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
            name: "ObservabilityAgent",
            instructions: "You are a helpful assistant."
        );

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\n✅ Agent ready with observability logging!");
        Console.WriteLine($"✅ Logging to: {outputPath}\n");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("INTERACTIVE MODE");
        Console.WriteLine(new string('=', 75));
        Console.WriteLine("Try: 'tell me a joke' or 'what is 2+2?'");
        Console.WriteLine("Type 'quit' to exit\n");

        await File.WriteAllTextAsync(outputPath, $"=== Observability Log Started at {DateTime.Now} ===\n\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                break;
            }

            var requestTime = DateTime.Now;
            await File.AppendAllTextAsync(outputPath, $"[{requestTime:yyyy-MM-dd HH:mm:ss}] USER: {userInput}\n");

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

            Console.Write("\nAgent: ");

            ThreadRun run = await client.Runs.CreateRunAsync(
                thread: thread,
                agent: agent
            );

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(1000);
                run = await client.Runs.GetRunAsync(thread.Id, run.Id);
            }

            var messages = client.Messages.GetMessagesAsync(
                threadId: thread.Id,
                order: ListSortOrder.Descending,
                limit: 1
            );

            var agentResponse = "";
            await foreach (var message in messages)
            {
                if (message.Role == MessageRole.Agent)
                {
                    foreach (var content in message.ContentItems)
                    {
                        if (content is MessageTextContent textContent)
                        {
                            agentResponse = textContent.Text.Trim();
                            Console.WriteLine(agentResponse);
                        }
                    }
                    break;
                }
            }

            var responseTime = DateTime.Now;
            var duration = (responseTime - requestTime).TotalSeconds;

            await File.AppendAllTextAsync(outputPath, $"[{responseTime:yyyy-MM-dd HH:mm:ss}] AGENT: {agentResponse}\n");
            await File.AppendAllTextAsync(outputPath, $"[{responseTime:yyyy-MM-dd HH:mm:ss}] METRICS: Duration={duration:F2}s, Prompt Tokens={run.Usage?.PromptTokens ?? 0}, Completion Tokens={run.Usage?.CompletionTokens ?? 0}\n\n");

            Console.WriteLine($"\n📊 Duration: {duration:F2}s | Prompt Tokens: {run.Usage?.PromptTokens ?? 0} | Completion Tokens: {run.Usage?.CompletionTokens ?? 0}");
            Console.WriteLine();
        }

        Console.WriteLine($"\n✅ Complete log saved to: {outputPath}");
        Console.WriteLine($"📊 Check the file for full observability data!");

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
