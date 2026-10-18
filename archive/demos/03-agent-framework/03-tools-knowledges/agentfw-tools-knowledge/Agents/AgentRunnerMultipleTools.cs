using Azure;
using Azure.AI.OpenAI;
using AgentFwToolsKnowledge.Models;
using AgentFwToolsKnowledge.Tools;
using OpenAI.Chat;
using System.Text.Json;

namespace AgentFwToolsKnowledge.Agents;

public class AgentRunnerMultipleTools(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("🛠️ DEMO: Multiple Function Tools");
        Console.WriteLine(new string('=', 70));

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

        // Define tools
        var weatherTool = ChatTool.CreateFunctionTool(
            functionName: "get_weather",
            functionDescription: "Get current weather for a location",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "location": {
                            "type": "string",
                            "description": "City name"
                        }
                    },
                    "required": ["location"]
                }
                """)
        );

        var calculateTool = ChatTool.CreateFunctionTool(
            functionName: "calculate",
            functionDescription: "Calculate a mathematical expression",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expression": {
                            "type": "string",
                            "description": "Math expression"
                        }
                    },
                    "required": ["expression"]
                }
                """)
        );

        var timeTool = ChatTool.CreateFunctionTool(
            functionName: "get_time",
            functionDescription: "Get current time in a timezone",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "timezone": {
                            "type": "string",
                            "description": "Timezone like 'America/New_York' or 'Europe/London'"
                        }
                    },
                    "required": ["timezone"]
                }
                """)
        );

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a helpful assistant with weather, calculator, and time tools. " +
                "Choose the right tool automatically. " +
                "For math explanations, format key equations using LaTeX display math blocks with \\[ and \\], " +
                "and use inline LaTeX with \\( and \\) for short expressions. " +
                "Do not use code fences; keep the prose concise."
            )
        };

        Console.WriteLine("\n✅ Agent created with 3 tools:");
        Console.WriteLine("   🌤️  Weather tool");
        Console.WriteLine("   🧮 Calculator tool");
        Console.WriteLine("   ⏰ Time zone tool");

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
            chatOptions.Tools.Add(weatherTool);
            chatOptions.Tools.Add(calculateTool);
            chatOptions.Tools.Add(timeTool);

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
                                var args = JsonDocument.Parse(toolCall.FunctionArguments);
                                string result;

                                switch (toolCall.FunctionName)
                                {
                                    case "get_weather":
                                        var location = args.RootElement.GetProperty("location").GetString() ?? "";
                                        result = GetWeather(location);
                                        break;

                                    case "calculate":
                                        var expression = args.RootElement.GetProperty("expression").GetString() ?? "";
                                        result = Calculate(expression);
                                        break;

                                    case "get_time":
                                        var timezone = args.RootElement.GetProperty("timezone").GetString() ?? "";
                                        result = await GetTimeAsync(timezone);
                                        break;

                                    default:
                                        result = "Unknown tool";
                                        break;
                                }

                                messages.Add(new ToolChatMessage(toolCall.Id, result));
                            }

                            requiresAction = true;
                            break;
                        }
                }
            } while (requiresAction);

            Console.WriteLine("\n");
        }
    }

    private static string GetWeather(string location)
    {
        var weatherData = new Dictionary<string, string>
        {
            ["london"] = "🌧️ 15°C, Rainy",
            ["paris"] = "☀️ 22°C, Sunny",
            ["tokyo"] = "⛅ 18°C, Partly Cloudy",
            ["new york"] = "🌤️ 20°C, Clear"
        };

        return weatherData.TryGetValue(location.ToLower(), out var weather)
            ? weather
            : $"Weather data not available for {location}";
    }

    private static string Calculate(string expression)
    {
        return SafeCalculator.Evaluate(expression);
    }

    private static async Task<string> GetTimeAsync(string timezone)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync($"http://worldtimeapi.org/api/timezone/{timezone}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var datetime = doc.RootElement.GetProperty("datetime").GetString() ?? "";
                var time = datetime.Split('T')[1].Split('.')[0];
                return $"⏰ Current time in {timezone}: {time}";
            }

            return $"Could not get time for {timezone}";
        }
        catch
        {
            return $"Error getting time for {timezone}";
        }
    }
}
