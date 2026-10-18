using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentKnowledgeTools.Models;
using AgentKnowledgeTools.Utilities;

namespace AgentKnowledgeTools.Agents;

public sealed class AgentRunnerCodeInterpreter(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}\n");

        string assetFilePath = Path.Combine(AppContext.BaseDirectory, "assets", "quarterly_results.csv");

        var agentsClient = new PersistentAgentsClient(
            config.ProjectConnectionString,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = true
            })
        );

        PersistentAgentFileInfo file = await agentsClient.Files.UploadFileAsync(
            filePath: assetFilePath,
            purpose: PersistentAgentFilePurpose.Agents
        );
        Console.WriteLine($"Uploaded file, file ID: {file.Id}");

        var codeInterpreterTool = new CodeInterpreterToolDefinition();

        PersistentAgent agent = await agentsClient.Administration.CreateAgentAsync(
            model: config.Model,
            name: "code-interpreter-agent",
            instructions: "You are a helpful agent with access to code interpreter tools. Use the code interpreter to analyze uploaded files and create visualizations as requested. Provide images directly in your response.",
            description: "Demonstrates Code Interpreter tool for analyzing CSV files and generating data visualizations (charts/graphs) in a sandboxed Python environment.",
            tools: [codeInterpreterTool],
            toolResources: new ToolResources
            {
                CodeInterpreter = new CodeInterpreterToolResource
                {
                    FileIds = { file.Id }
                }
            }
        );

        Console.WriteLine($"Created agent, agent ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
            threadId: thread.Id,
            role: MessageRole.User,
            content: "Could you please create a bar chart for the operating profit in the TRANSPORTATION sector from the uploaded csv file and display it as an image?"
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

        Console.WriteLine($"Run finished with status: {run.Status}");

        if (run.Status == RunStatus.Failed)
        {
            Console.WriteLine($"Run failed: {run.LastError?.Message}");
        }

        var messages = agentsClient.Messages.GetMessagesAsync(thread.Id);
        await foreach (var msg in messages)
        {
            if (msg.Role == MessageRole.Agent)
            {
                foreach (var content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        Console.WriteLine($"Last Message: {textContent.Text}");
                    }
                    else if (content is MessageImageFileContent imageContent)
                    {
                        Console.WriteLine($"Image file ID: {imageContent.FileId}");
                        var downloadedPath = await FileDownloadUtility.DownloadFileToDirectoryAsync(
                            agentsClient,
                            imageContent.FileId,
                            "downloads"
                        );
                        if (downloadedPath != null)
                        {
                            Console.WriteLine($"Downloaded image to: {downloadedPath}");
                        }
                    }
                }
                break;
            }
        }

        await agentsClient.Files.DeleteFileAsync(file.Id);
        Console.WriteLine("Deleted file");

        if (config.DeleteAgentOnExit)
        {
            await agentsClient.Administration.DeleteAgentAsync(agent.Id);
            Console.WriteLine("Deleted agent");
        }
        else
        {
            Console.WriteLine($"Agent {agent.Id} preserved for examination in Azure AI Foundry");
        }
    }
}
