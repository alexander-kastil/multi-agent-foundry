# Agent Framework Basics - C# Demos

This project contains 11 demos showcasing Azure Agent Framework capabilities in C#. These demos are C# ports of the Python demos in `agentfw_basics-py`.

## Prerequisites

- .NET 10.0 SDK
- Azure AI Foundry project with deployed models
- Azure CLI (for authentication)
- Optional: Azure OpenAI endpoint and API key (for demo #2)

## Configuration

Update `appsettings.json` with your Azure AI Foundry and Azure OpenAI settings:

```json
{
  "AzureAIProjectEndpoint": "https://your-resource.services.ai.azure.com/api/projects/your-project",
  "AzureAIModelDeploymentName": "gpt-4o-mini",
  "AzureOpenAIEndpoint": "https://your-openai.openai.azure.com",
  "AzureOpenAIChatDeploymentName": "gpt-4o",
  "AzureOpenAIApiKey": "your-api-key",
  "AzureOpenAIApiVersion": "2025-01-01-preview",
  "DataPath": "./data",
  "OutputPath": "./output",
  "AzureAIAgentId": ""
}
```

## Running the Demos

```bash
dotnet run
```

This will display a menu with 11 demo options:

## Available Demos

### 1. Create Agent
Creates a new Azure AI Foundry agent and demonstrates basic interactive chat functionality.

**Key Concepts:**
- Creating agents with the Persistent Agents API
- Managing threads for conversation context
- Interactive chat loops

### 2. OpenAI Chat
Direct Azure OpenAI chat without using the Agent Service, demonstrating the lower-level API.

**Key Concepts:**
- Using Azure.AI.OpenAI directly
- Streaming responses
- Managing conversation history with size limits

### 3. Chat History
Demonstrates how the Persistent Agent maintains chat history across multiple exchanges.

**Key Concepts:**
- Thread-based conversation history
- Retrieving and displaying message history
- Message ordering and pagination

### 4. Streaming
Shows response streaming capabilities for real-time token-by-token output.

**Key Concepts:**
- Streaming API usage
- Real-time response rendering
- MessageContentUpdate handling

### 5. Threading
Demonstrates thread serialization and deserialization with auto-save to JSON.

**Key Concepts:**
- Thread persistence across sessions
- JSON serialization of thread state
- Restoring previous conversations

### 6. Structured Output
Extracts structured data from natural language using JSON schemas.

**Key Concepts:**
- Structured output with Pydantic-like models
- JSON parsing and validation
- Data extraction from unstructured text

### 7. Middleware
Simulates middleware concepts for logging and timing.

**Key Concepts:**
- Request/response interception patterns
- Timing measurements
- Logging wrappers

### 8. Observability
Demonstrates request/response logging and metrics collection.

**Key Concepts:**
- Token usage tracking
- Request/response logging to files
- Performance metrics collection

### 9. Multimodal
Shows concepts for multimodal content (image analysis) with code examples.

**Key Concepts:**
- File upload patterns
- Vision-capable model usage
- Multimodal message attachments

### 10. Long Term Memory
AI-powered long-term memory with file persistence across sessions.

**Key Concepts:**
- Persistent user profile storage
- Manual fact recording
- Cross-session memory retention

### 11. Use Existing Agent
Connects to an existing Azure AI Foundry agent by ID.

**Key Concepts:**
- Reusing pre-configured agents
- Agent retrieval by ID
- Connecting to production agents

## Implementation Notes

### Differences from Python Version

The Python demos use the `agent_framework` package which provides high-level abstractions. The C# implementation uses:

- **Azure.AI.Agents.Persistent**: For Azure AI Foundry agent interactions (similar to Python's AzureAIAgentClient)
- **Azure.AI.OpenAI**: For direct Azure OpenAI calls (similar to Python's AzureOpenAIChatClient)

Some features have simplified implementations:
- **Middleware**: Simulated through timing/logging wrappers (no native middleware API)
- **Observability**: File-based logging instead of full OpenTelemetry integration
- **Multimodal**: Conceptual demo showing the API patterns (requires vision model configuration)

### Error Handling

All demos include:
- Proper exception handling
- Input validation
- Graceful degradation when optional features are unavailable

### Resource Management

Demos clean up created resources (agents, threads) after execution to avoid leaving artifacts in your Azure AI Foundry project.

## Project Structure

```
agentfw_basics/
├── Agents/
│   ├── AgentRunnerCreateAgent.cs
│   ├── AgentRunnerOpenAIChat.cs
│   ├── AgentRunnerChatHistory.cs
│   ├── AgentRunnerStreaming.cs
│   ├── AgentRunnerThreading.cs
│   ├── AgentRunnerStructuredOutput.cs
│   ├── AgentRunnerMiddleware.cs
│   ├── AgentRunnerObservability.cs
│   ├── AgentRunnerMultimodal.cs
│   ├── AgentRunnerLongTermMemory.cs
│   └── AgentRunnerUseExistingAgent.cs
├── Models/
│   └── AppConfig.cs
├── Program.cs
├── appsettings.json
└── agentfw_basics.csproj
```

## Output Files

Some demos create output files in the `./output` directory:
- **Threading**: `thread_history.json` - Thread state for restoration
- **Observability**: `observability_log.txt` - Request/response logs
- **Long Term Memory**: `user_memory.json` - Persistent user profile

## Troubleshooting

**Authentication Issues:**
- Ensure you're logged in with `az login`
- Verify your Azure subscription has access to the AI Foundry project

**Missing Configuration:**
- Demo #2 requires Azure OpenAI credentials
- Demo #11 requires an existing agent ID in configuration

**Build Errors:**
- Ensure .NET 10.0 SDK is installed
- Run `dotnet restore` to restore packages

## Further Reading

- [Azure AI Agents Documentation](https://learn.microsoft.com/azure/ai-services/agents/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Python Agent Framework Comparison](../agentfw_basics-py/readme.md)
