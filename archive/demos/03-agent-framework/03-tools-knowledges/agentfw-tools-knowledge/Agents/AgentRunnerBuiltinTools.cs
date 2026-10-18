using Azure;
using Azure.AI.OpenAI;
using AgentFwToolsKnowledge.Models;
using OpenAI.Chat;

namespace AgentFwToolsKnowledge.Agents;

/// <summary>
/// Built-in Tools Demo - Code Interpreter and Web Search
/// 
/// ⚠️ IMPORTANT LIMITATION:
/// This demo attempts to use built-in tools with ChatCompletion API,
/// but these tools are NOT SUPPORTED with the standard Chat API.
/// 
/// Built-in tools (Code Interpreter, Web Search) only work with:
/// - Azure AI Agents Service (PersistentAgentsClient)
/// - Specific Azure AI Response APIs
/// 
/// This file is kept for educational purposes to show what doesn't work
/// with the standard Azure OpenAI Chat API.
/// </summary>
public class AgentRunnerBuiltinTools(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("🛠️ DEMO: Built-in Tools - Code Interpreter & Web Search");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("""

            ⚠️ IMPORTANT LIMITATION:

            This demo shows the concept of built-in tools, but note that
            Code Interpreter and Web Search are NOT available with the
            standard Azure OpenAI Chat Completion API.

            These built-in tools only work with:
            1. Azure AI Agents Service (see FileSearch demo for example)
            2. Specific Azure AI Response APIs

            This demo will work as a regular chat without the built-in tools.
            For working file search, see the FileSearch demo.

            Built-in tools that would be available in Azure AI Agents:
            1. 🐍 CODE INTERPRETER
               - Execute Python code in a sandboxed environment
               - Perform data analysis, calculations, and visualizations
               - Handle complex mathematical computations

            2. 🌐 WEB SEARCH (via Bing Grounding)
               - Search the web for current information
               - Get real-time data and facts
               - Answer questions about recent events

            3. 🔍 FILE SEARCH
               - Search through documents in vector stores
               - Find relevant information in uploaded files
               - Answer questions based on document content
            """);

        if (string.IsNullOrWhiteSpace(config.AzureOpenAIEndpoint) || string.IsNullOrWhiteSpace(config.AzureOpenAIApiKey))
        {
            Console.WriteLine("\n❌ Azure OpenAI configuration is missing.");
            return;
        }

        var client = new AzureOpenAIClient(
            new Uri(config.AzureOpenAIEndpoint),
            new AzureKeyCredential(config.AzureOpenAIApiKey)
        );

        var chatClient = client.GetChatClient(config.AzureOpenAIChatDeploymentName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a helpful assistant. " +
                "When presenting formulas or equations, use LaTeX with \\[ and \\] for display math " +
                "and \\( and \\) for inline math. Keep explanations concise."
            )
        };

        Console.WriteLine("\n✅ Chat agent created (without built-in tools due to API limitations)");

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("💬 Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70) + "\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\n👋 Goodbye!");
                break;
            }

            messages.Add(new UserChatMessage(userInput));
            Console.Write("Agent: ");

            var completionOptions = new ChatCompletionOptions();
            var streamingCompletion = chatClient.CompleteChatStreamingAsync(messages, completionOptions);

            await foreach (var update in streamingCompletion)
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    Console.Write(contentPart.Text);
                }
            }

            Console.WriteLine("\n");
        }
    }
}
