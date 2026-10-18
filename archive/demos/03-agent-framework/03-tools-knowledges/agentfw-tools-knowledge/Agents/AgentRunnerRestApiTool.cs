using Azure;
using Azure.AI.OpenAI;
using AgentFwToolsKnowledge.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace AgentFwToolsKnowledge.Agents;

public class AgentRunnerRestApiTool(AppConfig config)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("✅ DEMO: REST API Tools - DummyJSON Todos");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"""

            This demo shows how to create custom tools that call REST APIs.

            API: {config.RestApiBaseUrl}/todos

            Available Tools:
            1. get_todos - Get all todos with pagination
            2. get_todo_by_id - Get a specific todo by ID
            3. get_todos_by_user - Get all todos for a specific user

            Example queries:
            - "Show me the first 5 todos"
            - "Get todo with ID 10"
            - "Show me all todos for user 5"
            - "List todos from 10 to 20"
            - "What's in todo number 1?"
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

        // Define REST API tools
        var getTodosTool = ChatTool.CreateFunctionTool(
            functionName: "get_todos",
            functionDescription: "Get all todos with optional pagination",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "limit": {
                            "type": "integer",
                            "description": "Number of todos to retrieve (default 10)"
                        },
                        "skip": {
                            "type": "integer",
                            "description": "Number of todos to skip for pagination"
                        }
                    }
                }
                """)
        );

        var getTodoByIdTool = ChatTool.CreateFunctionTool(
            functionName: "get_todo_by_id",
            functionDescription: "Get a specific todo by its ID",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "todo_id": {
                            "type": "integer",
                            "description": "The ID of the todo item to get"
                        }
                    },
                    "required": ["todo_id"]
                }
                """)
        );

        var getTodosByUserTool = ChatTool.CreateFunctionTool(
            functionName: "get_todos_by_user",
            functionDescription: "Get all todos for a specific user",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "user_id": {
                            "type": "integer",
                            "description": "The user ID to get todos for"
                        }
                    },
                    "required": ["user_id"]
                }
                """)
        );

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a helpful assistant with access to a todos API. " +
                "Use the tools to help users browse todos, get specific todo details, and find todos by user. " +
                "Always provide clear and helpful responses. " +
                "If the API is unavailable, inform the user politely and suggest trying again later."
            )
        };

        Console.WriteLine("\n✅ Agent created with REST API tools");

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
            chatOptions.Tools.Add(getTodosTool);
            chatOptions.Tools.Add(getTodoByIdTool);
            chatOptions.Tools.Add(getTodosByUserTool);

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
                                    case "get_todos":
                                        var limit = args.RootElement.TryGetProperty("limit", out var limitProp) 
                                            ? limitProp.GetInt32() : 10;
                                        var skip = args.RootElement.TryGetProperty("skip", out var skipProp) 
                                            ? skipProp.GetInt32() : 0;
                                        result = await GetTodosAsync(limit, skip);
                                        break;

                                    case "get_todo_by_id":
                                        var todoId = args.RootElement.GetProperty("todo_id").GetInt32();
                                        result = await GetTodoByIdAsync(todoId);
                                        break;

                                    case "get_todos_by_user":
                                        var userId = args.RootElement.GetProperty("user_id").GetInt32();
                                        result = await GetTodosByUserAsync(userId);
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

    private async Task<string> GetTodosAsync(int limit = 10, int skip = 0)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync($"{config.RestApiBaseUrl}/todos?limit={limit}&skip={skip}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var todos = doc.RootElement.GetProperty("todos");
                var total = doc.RootElement.GetProperty("total").GetInt32();

                if (todos.GetArrayLength() == 0)
                    return "No todos found.";

                var result = $"Todos (showing {todos.GetArrayLength()} of {total}):\n";
                foreach (var todo in todos.EnumerateArray())
                {
                    var id = todo.GetProperty("id").GetInt32();
                    var task = todo.GetProperty("todo").GetString() ?? "Unknown";
                    var completed = todo.GetProperty("completed").GetBoolean() ? "✓" : "✗";
                    var userId = todo.GetProperty("userId").GetInt32();
                    result += $"- [{completed}] #{id}: {task} (User: {userId})\n";
                }

                return result;
            }

            return $"Error: API returned status {response.StatusCode}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out. The API might be unavailable.";
        }
        catch (Exception ex)
        {
            return $"Error: Could not connect to DummyJSON API - {ex.Message}";
        }
    }

    private async Task<string> GetTodoByIdAsync(int todoId)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync($"{config.RestApiBaseUrl}/todos/{todoId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var todo = JsonDocument.Parse(json).RootElement;
                var completed = todo.GetProperty("completed").GetBoolean() ? "✓ Completed" : "✗ Not completed";
                
                var result = "Todo Details:\n";
                result += $"ID: {todo.GetProperty("id").GetInt32()}\n";
                result += $"Task: {todo.GetProperty("todo").GetString()}\n";
                result += $"Status: {completed}\n";
                result += $"User ID: {todo.GetProperty("userId").GetInt32()}\n";
                return result;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return $"Todo with ID {todoId} not found.";

            return $"Error: API returned status {response.StatusCode}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out. The API might be unavailable.";
        }
        catch (Exception ex)
        {
            return $"Error: Could not connect to DummyJSON API - {ex.Message}";
        }
    }

    private async Task<string> GetTodosByUserAsync(int userId)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync($"{config.RestApiBaseUrl}/todos/user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var todos = doc.RootElement.GetProperty("todos");
                var total = doc.RootElement.GetProperty("total").GetInt32();

                if (todos.GetArrayLength() == 0)
                    return $"No todos found for user {userId}.";

                var result = $"Todos for User {userId} ({total} total):\n";
                foreach (var todo in todos.EnumerateArray())
                {
                    var id = todo.GetProperty("id").GetInt32();
                    var task = todo.GetProperty("todo").GetString() ?? "Unknown";
                    var completed = todo.GetProperty("completed").GetBoolean() ? "✓" : "✗";
                    result += $"- [{completed}] #{id}: {task}\n";
                }

                return result;
            }

            return $"Error: API returned status {response.StatusCode}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out. The API might be unavailable.";
        }
        catch (Exception ex)
        {
            return $"Error: Could not connect to DummyJSON API - {ex.Message}";
        }
    }
}
