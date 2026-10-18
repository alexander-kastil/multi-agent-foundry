using Azure;
using Azure.AI.OpenAI;
using AgentFwToolsKnowledge.Models;
using AgentFwToolsKnowledge.Tools;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;

namespace AgentFwToolsKnowledge.Agents;

public class AgentRunnerCalculator(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("🧮 DEMO: Function Tools - Calculator");
        Console.WriteLine(new string('=', 70));

        if (string.IsNullOrWhiteSpace(config.AzureOpenAIEndpoint) || string.IsNullOrWhiteSpace(config.AzureOpenAIApiKey))
        {
            Console.WriteLine("\n❌ Azure OpenAI configuration is missing.");
            Console.WriteLine("Please set AzureOpenAIEndpoint and AzureOpenAIApiKey in appsettings.json");
            return;
        }

        var client = new AzureOpenAIClient(
            new Uri(config.AzureOpenAIEndpoint),
            new AzureKeyCredential(config.AzureOpenAIApiKey)
        );

        var chatClient = client.GetChatClient(config.AzureOpenAIChatDeploymentName);

        // Define calculator tool
        var calculateTool = ChatTool.CreateFunctionTool(
            functionName: "calculate",
            functionDescription: "Evaluate a mathematical expression",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expression": {
                            "type": "string",
                            "description": "Mathematical expression to evaluate, e.g. '2 + 2' or '10 * 5'"
                        }
                    },
                    "required": ["expression"]
                }
                """)
        );

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a math assistant. Always use the calculate tool for all math problems. " +
                "When presenting formulas, derivations, or key results, format math using LaTeX: " +
                "use display math with \\[ and \\] for multi-line or important equations, and inline math with \\( and \\) for short expressions. " +
                "Keep explanations concise and avoid code fences."
            )
        };

        Console.WriteLine("\n✅ Agent created with calculator tool");
        Console.WriteLine("💡 TIP: Ask math questions or calculations");

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

            var chatOptions = new ChatCompletionOptions();
            chatOptions.Tools.Add(calculateTool);

            bool requiresAction;
            do
            {
                requiresAction = false;
                var completion = await chatClient.CompleteChatAsync(messages, chatOptions);

                switch (completion.Value.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        {
                            var responseMessage = completion.Value.Content[0].Text;
                            messages.Add(new AssistantChatMessage(completion.Value));
                            Console.Write(responseMessage);
                            break;
                        }

                    case ChatFinishReason.ToolCalls:
                        {
                            messages.Add(new AssistantChatMessage(completion.Value));

                            foreach (var toolCall in completion.Value.ToolCalls)
                            {
                                if (toolCall.FunctionName == "calculate")
                                {
                                    var args = JsonDocument.Parse(toolCall.FunctionArguments);
                                    var expression = args.RootElement.GetProperty("expression").GetString() ?? "";

                                    var result = Calculate(expression);
                                    messages.Add(new ToolChatMessage(toolCall.Id, result));
                                }
                            }

                            requiresAction = true;
                            break;
                        }
                }
            } while (requiresAction);

            Console.WriteLine("\n");
        }
    }

    private static string Calculate(string expression)
    {
        return SafeCalculator.Evaluate(expression);
    }
}
