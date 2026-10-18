# Agent Framework Tools & Knowledge - C# Demos

This project contains 6 demos showcasing Azure Agent Framework tools and knowledge capabilities in C#. These demos are C# ports of the Python demos in `agentfw_tools-knowledge-py`.

## Prerequisites

- .NET 10.0 SDK
- Azure AI Foundry project with deployed models
- Azure CLI (for authentication)
- Azure OpenAI endpoint and API key
- Optional: Vector Store ID (for File Search demo)

## Configuration

**IMPORTANT:** Before running the demos, you must update `appsettings.json` with your actual Azure credentials and endpoints.

Update `appsettings.json` with your Azure AI Foundry and Azure OpenAI settings:

```json
{
  "AzureAIProjectEndpoint": "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/YOUR-PROJECT",
  "AzureAIModelDeploymentName": "gpt-4o-mini",
  "AzureOpenAIEndpoint": "https://YOUR-RESOURCE.openai.azure.com/",
  "AzureOpenAIChatDeploymentName": "gpt-4o",
  "AzureOpenAIApiKey": "YOUR-API-KEY-HERE",
  "AzureOpenAIApiVersion": "2025-01-01-preview",
  "DataPath": "./data",
  "OutputPath": "./output",
  "VectorStoreId": "YOUR-VECTOR-STORE-ID",
  "RestApiBaseUrl": "https://dummyjson.com"
}
```

## Running the Demos

```bash
dotnet run
```

This will display a menu with 6 demo options.

## Available Demos

### 1. Calculator - Function Tool
Creates a custom calculator function tool that evaluates mathematical expressions.

**Key Concepts:**
- Custom function tools with Azure OpenAI Chat API
- Tool parameter definitions using JSON schemas
- Function calling and result handling
- Math expression evaluation

**Example queries:**
- "What is 15 * 23?"
- "Calculate the sum of 100, 200, and 300"
- "What's 2^10?"

### 2. Multiple Tools
Demonstrates multiple custom tools working together: weather, calculator, and time zone tools.

**Key Concepts:**
- Multiple tool registration
- Automatic tool selection by the agent
- Combining different tool types
- External API integration (World Time API)

**Example queries:**
- "What's the weather in London?"
- "What time is it in Europe/Paris?"
- "Calculate 50 + 25 and tell me the weather in Tokyo"

### 3. REST API Tool
Shows how to create custom tools that call external REST APIs (DummyJSON Todos API).

**Key Concepts:**
- REST API integration in tools
- Pagination handling
- Error handling and timeout management
- Structured API responses

**Example queries:**
- "Show me the first 5 todos"
- "Get todo with ID 10"
- "Show me all todos for user 5"
- "List todos from 10 to 20"

### 4. File Search
Uses Azure AI Agents Service with File Search tool to query documents in a vector store.

**Key Concepts:**
- File Search tool with vector stores
- Document-based knowledge retrieval
- Azure AI Agents Service integration
- Thread-based conversations

**Setup Required:**
1. Create a Vector Store in Azure AI Foundry portal
2. Upload documents (PDF, TXT, DOCX)
3. Copy the Vector Store ID to `appsettings.json`

**Example queries:**
- "Tell me about the documents in the store"
- "Search for information about [topic]"
- "What does the document say about [subject]?"

### 5. Built-in Tools (Educational)
Educational demo showing the concept of built-in tools (Code Interpreter, Web Search).

**⚠️ Important Note:**
This demo shows that built-in tools like Code Interpreter and Web Search are NOT available with the standard Azure OpenAI Chat Completion API. These tools only work with:
- Azure AI Agents Service (PersistentAgentsClient)
- Specific Azure AI Response APIs

This demo functions as a regular chat to demonstrate the limitation.

**Key Concepts:**
- Understanding API limitations
- Difference between Chat API and Agents API
- When to use which API

### 6. Human-in-the-Loop
Implements an approval system for dangerous operations (file deletion vs. safe file creation).

**Key Concepts:**
- Human approval workflows
- Safe vs. dangerous operations
- Interactive confirmation prompts
- File management operations

**Example commands:**
- "Create a file named test.txt with some content"
- "Delete test.txt" (will ask for approval)
- "Create file notes.txt saying 'Hello World'"
- "Delete notes.txt" (will ask for approval)

## Project Structure

```
agentfw-tools-knowledge/
├── Agents/
│   ├── AgentRunnerCalculator.cs
│   ├── AgentRunnerMultipleTools.cs
│   ├── AgentRunnerRestApiTool.cs
│   ├── AgentRunnerFileSearch.cs
│   ├── AgentRunnerBuiltinTools.cs
│   └── AgentRunnerHumanInTheLoop.cs
├── Models/
│   └── AppConfig.cs
├── Program.cs
├── appsettings.json
├── agentfw-tools-knowledge.csproj
└── readme.md
```

## Implementation Notes

### Differences from Python Version

The Python demos use the `agent_framework` package which provides high-level abstractions. The C# implementation uses:

- **Azure.AI.OpenAI**: For direct Azure OpenAI calls with function calling
- **Azure.AI.Agents.Persistent**: For Azure AI Foundry agent interactions (File Search demo)

Key differences:
- **Function Tools**: C# uses `ChatTool.CreateFunctionTool()` with JSON schema parameters
- **Tool Calling**: Manual tool call handling in C# vs. automatic in Python framework
- **API Choice**: Some demos use Chat Completion API, File Search uses Agents API

### Error Handling

All demos include:
- Proper exception handling
- Input validation
- Timeout management for external APIs
- Graceful degradation when features are unavailable

### Security

- **Safe Calculator**: Uses a custom `SafeCalculator` class that implements a safe mathematical expression parser, preventing code injection vulnerabilities that can occur with `DataTable.Compute()` or `eval()` functions
- **Input Validation**: All user inputs and API responses are validated before processing
- **Secret Management**: Configuration values should be stored securely and not committed to source control

### Resource Management

- File Search demo cleans up created resources (agents, threads)
- Human-in-the-Loop demo creates files in `./output` directory
- All HTTP clients properly disposed

## Output Files

Some demos create output files:
- **Human-in-the-Loop**: Files created/deleted in `./output` directory

## Troubleshooting

**Authentication Issues:**
- For File Search demo: Ensure you're logged in with `az login`
- Verify your Azure subscription has access to the AI Foundry project

**Missing Configuration:**
- All demos (except Built-in Tools) require Azure OpenAI credentials
- File Search demo requires a valid Vector Store ID
- REST API demo requires internet access to DummyJSON API

**Build Errors:**
- Ensure .NET 10.0 SDK is installed
- Run `dotnet restore` to restore packages

**API Timeout:**
- REST API and Time tools may timeout if external APIs are slow
- Timeout is set to 5-10 seconds by default

## Comparison with Python Demos

| Python Demo                         | C# Demo                  | Status      |
| ----------------------------------- | ------------------------ | ----------- |
| agentfw_function_tool_calculator.py | AgentRunnerCalculator    | ✅ Ported   |
| agentfw_multiple_tools.py           | AgentRunnerMultipleTools | ✅ Ported   |
| agentfw_rest_api_tool.py            | AgentRunnerRestApiTool   | ✅ Ported   |
| agentfw_file_search_tool.py         | AgentRunnerFileSearch    | ✅ Ported   |
| agentfw_builtin_tools.py            | AgentRunnerBuiltinTools  | ✅ Ported   |
| agentfw_human_in_the_loop.py        | AgentRunnerHumanInTheLoop| ✅ Ported   |
| agentfw_mcp_local.py                | -                        | ❌ Not ported (MCP specific) |
| agentfw_mcp_external.py             | -                        | ❌ Not ported (MCP specific) |

**Note:** MCP (Model Context Protocol) demos were not ported as they are Python-specific implementations.

## Further Reading

- [Azure AI Agents Documentation](https://learn.microsoft.com/azure/ai-services/agents/)
- [Azure OpenAI Function Calling](https://learn.microsoft.com/azure/ai-services/openai/how-to/function-calling)
- [Azure AI Foundry Vector Stores](https://learn.microsoft.com/azure/ai-services/agents/how-to/file-search)
- [Python Agent Framework Comparison](../agentfw_tools-knowledge-py/)
