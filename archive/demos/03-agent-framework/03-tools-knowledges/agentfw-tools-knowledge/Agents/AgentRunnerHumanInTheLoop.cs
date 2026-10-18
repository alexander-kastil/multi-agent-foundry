using Azure;
using Azure.AI.OpenAI;
using AgentFwToolsKnowledge.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace AgentFwToolsKnowledge.Agents;

/// <summary>
/// Human-in-the-Loop Approval Demo
/// 
/// Simple example with 2 functions:
/// 1. create_file() - No approval needed (safe operation)
/// 2. delete_file() - Requires approval (dangerous operation)
/// </summary>
public class AgentRunnerHumanInTheLoop
{
    private readonly AppConfig _config;
    private readonly string _outputDir;

    public AgentRunnerHumanInTheLoop(AppConfig config)
    {
        _config = config;
        _outputDir = config.OutputPath;
        Directory.CreateDirectory(_outputDir);
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("🔒 DEMO: Human-in-the-Loop - Create vs Delete");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\n📋 This demo has 2 functions:");
        Console.WriteLine("   ✅ create_file() - Runs immediately (no approval)");
        Console.WriteLine("   🔒 delete_file() - Requires your approval first");

        if (string.IsNullOrWhiteSpace(_config.AzureOpenAIEndpoint) || string.IsNullOrWhiteSpace(_config.AzureOpenAIApiKey))
        {
            Console.WriteLine("\n❌ Azure OpenAI configuration is missing.");
            return;
        }

        var client = new AzureOpenAIClient(
            new Uri(_config.AzureOpenAIEndpoint),
            new AzureKeyCredential(_config.AzureOpenAIApiKey)
        );

        var chatClient = client.GetChatClient(_config.AzureOpenAIChatDeploymentName);

        // Define tools
        var createFileTool = ChatTool.CreateFunctionTool(
            functionName: "create_file",
            functionDescription: "Create a new file with content. Safe operation - no approval needed.",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "filename": {
                            "type": "string",
                            "description": "Name of file to create"
                        },
                        "content": {
                            "type": "string",
                            "description": "Content to write in file"
                        }
                    },
                    "required": ["filename", "content"]
                }
                """)
        );

        var deleteFileTool = ChatTool.CreateFunctionTool(
            functionName: "delete_file",
            functionDescription: "Delete a file. This function requires user approval.",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "filename": {
                            "type": "string",
                            "description": "Name of file to delete"
                        }
                    },
                    "required": ["filename"]
                }
                """)
        );

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                """
                You are a file management assistant with access to file operations.

                IMPORTANT: You MUST call the functions directly. Do NOT ask the user for permission in chat.

                Rules:
                1. When user asks to create a file: IMMEDIATELY call create_file() function
                2. When user asks to delete a file: IMMEDIATELY call delete_file() function
                3. Do NOT ask for confirmation in the chat - the system will handle approvals automatically
                4. Just call the function and report the result
                """
            )
        };

        Console.WriteLine($"\n✅ Agent created with 2 functions");
        Console.WriteLine($"📁 Files will be created in: {Path.GetFullPath(_outputDir)}/");

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("💬 Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\n💡 Try these commands:");
        Console.WriteLine("   • Create a file named test.txt with some content");
        Console.WriteLine("   • Delete test.txt");
        Console.WriteLine("   • Create file notes.txt saying 'Hello World'");
        Console.WriteLine("   • Delete notes.txt");

        while (true)
        {
            Console.Write("\nYou: ");
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
            chatOptions.Tools.Add(createFileTool);
            chatOptions.Tools.Add(deleteFileTool);

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
                                    case "create_file":
                                        var createFilename = args.RootElement.GetProperty("filename").GetString() ?? "";
                                        var content = args.RootElement.GetProperty("content").GetString() ?? "";
                                        result = CreateFile(createFilename, content);
                                        break;

                                    case "delete_file":
                                        var deleteFilename = args.RootElement.GetProperty("filename").GetString() ?? "";
                                        result = DeleteFileWithApproval(deleteFilename, args);
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

            Console.WriteLine();
        }
    }

    private string CreateFile(string filename, string content)
    {
        try
        {
            var filePath = Path.Combine(_outputDir, filename);
            File.WriteAllText(filePath, content);
            return $"✅ File '{filename}' created successfully with {content.Length} characters";
        }
        catch (Exception ex)
        {
            return $"❌ Error creating file: {ex.Message}";
        }
    }

    private string DeleteFileWithApproval(string filename, JsonDocument args)
    {
        // Show approval request
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("🚨 APPROVAL REQUIRED");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"📝 Function: delete_file");
        Console.WriteLine($"📊 Arguments:");
        Console.WriteLine($"   - filename: {filename}");
        Console.WriteLine(new string('-', 70));

        // Ask for approval
        while (true)
        {
            Console.Write("⚠️ Do you want to APPROVE this action? (yes/no): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response is "yes" or "y")
            {
                Console.WriteLine("✅ APPROVED: Executing delete_file");
                return DeleteFile(filename);
            }
            else if (response is "no" or "n")
            {
                Console.WriteLine("❌ REJECTED: Not executing delete_file");
                return $"⛔ Function 'delete_file' was rejected by the user.";
            }
            else
            {
                Console.WriteLine("   Please enter 'yes' or 'no'");
            }
        }
    }

    private string DeleteFile(string filename)
    {
        try
        {
            var filePath = Path.Combine(_outputDir, filename);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return $"🗑️ File '{filename}' deleted successfully";
            }
            else
            {
                return $"⚠️ File '{filename}' not found in {_outputDir}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error deleting file: {ex.Message}";
        }
    }
}
