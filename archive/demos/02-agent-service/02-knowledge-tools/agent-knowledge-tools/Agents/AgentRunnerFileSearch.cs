using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentKnowledgeTools.Models;

namespace AgentKnowledgeTools.Agents;

public sealed class AgentRunnerFileSearch(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine($"Using endpoint: {config.ProjectConnectionString}");
        Console.WriteLine($"Using model: {config.Model}");
        Console.WriteLine($"Using vector store: {config.VectorStoreId}\n");

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
            name: "file-search-agent",
            instructions: "You are a helpful agent that can search through documents to answer questions. Use the file search tool to find relevant information.",
            description: "Demonstrates file search capabilities using a vector store to answer questions about documents.",
            tools: [new FileSearchToolDefinition()],
            toolResources: new ToolResources
            {
                FileSearch = new FileSearchToolResource
                {
                    VectorStoreIds = { config.VectorStoreId }
                }
            }
        );

        Console.WriteLine($"Created agent: {agent.Name}, ID: {agent.Id}");

        PersistentAgentThread thread = await agentsClient.Threads.CreateThreadAsync();
        Console.WriteLine($"Created thread, thread ID: {thread.Id}");

        PersistentThreadMessage message = await agentsClient.Messages.CreateMessageAsync(
            threadId: thread.Id,
            role: MessageRole.User,
            content: "Tell me about Equinox Gold"
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

        if (config.DeleteAgentOnExit)
        {
            await agentsClient.Administration.DeleteAgentAsync(agent.Id);
            Console.WriteLine("Deleted agent");
        }
        else
        {
            Console.WriteLine($"Agent {agent.Id} preserved for examination in Azure AI Foundry");
        }

        var messages = agentsClient.Messages.GetMessagesAsync(
            threadId: thread.Id,
            order: ListSortOrder.Ascending
        );

        await foreach (var msg in messages)
        {
            if (msg.ContentItems.Count > 0)
            {
                var lastTextContent = msg.ContentItems.OfType<MessageTextContent>().LastOrDefault();
                if (lastTextContent != null)
                {
                    Console.WriteLine($"{msg.Role}: {lastTextContent.Text.Trim()}");
                }
            }
        }
    }
}
