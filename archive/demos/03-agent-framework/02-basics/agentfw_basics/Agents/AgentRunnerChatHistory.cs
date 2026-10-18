using Azure.AI.Agents.Persistent;
using Azure.Identity;
using AgentFwBasics.Models;

namespace AgentFwBasics.Agents;

public class AgentRunnerChatHistory(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO: Chat History Management");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\nThis demo shows how the Persistent Agent maintains chat history");
        Console.WriteLine("across multiple exchanges in a thread.");

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
            name: "ChatHistoryAgent",
            instructions: "You are a friendly assistant. Answer clearly and concisely. Remember what the user tells you."
        );

        PersistentAgentThread thread = await client.Threads.CreateThreadAsync();

        Console.WriteLine("\nAgent created and thread initialized.");
        Console.WriteLine("Type 'quit' to exit\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\nGoodbye!");
                break;
            }

            await client.Messages.CreateMessageAsync(
                threadId: thread.Id,
                role: MessageRole.User,
                content: userInput
            );

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

            await foreach (var message in messages)
            {
                if (message.Role == MessageRole.Agent)
                {
                    foreach (var content in message.ContentItems)
                    {
                        if (content is MessageTextContent textContent)
                        {
                            Console.WriteLine($"Assistant: {textContent.Text.Trim()}");
                        }
                    }
                    break;
                }
            }

            var allMessages = client.Messages.GetMessagesAsync(
                threadId: thread.Id,
                order: ListSortOrder.Ascending
            );

            var messageList = new List<PersistentThreadMessage>();
            await foreach (var msg in allMessages)
            {
                messageList.Add(msg);
            }

            Console.WriteLine($"\n- Number of messages in thread: {messageList.Count}");
            for (int i = 0; i < messageList.Count; i++)
            {
                var msg = messageList[i];
                var role = msg.Role == MessageRole.User ? "user" : "assistant";
                var text = "";
                foreach (var content in msg.ContentItems)
                {
                    if (content is MessageTextContent textContent)
                    {
                        text = textContent.Text.Length > 200 
                            ? textContent.Text.Substring(0, 200) + "..." 
                            : textContent.Text;
                    }
                }
                Console.WriteLine($"  {i + 1}. {role}: {text}");
            }

            Console.WriteLine("\n" + new string('-', 60) + "\n");
        }

        await client.Threads.DeleteThreadAsync(thread.Id);
        await client.Administration.DeleteAgentAsync(agent.Id);
    }
}
