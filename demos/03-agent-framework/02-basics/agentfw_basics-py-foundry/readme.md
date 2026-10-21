# Azure AI Foundry SDK Demos

This folder contains demos migrated from Agent Framework to use Azure AI Foundry SDK directly (`azure-ai-projects`, `azure-ai-agents`, `azure-ai-inference`).

## Overview

These demos show how to build agent-based applications using the Azure AI Foundry SDK without the Agent Framework abstraction layer. This provides more direct control over Azure AI services while maintaining similar functionality.

## Key Differences from Agent Framework

| Aspect | Agent Framework | Azure AI Foundry SDK |
|--------|----------------|---------------------|
| **Abstraction** | High-level abstractions (ChatAgent, etc.) | Direct SDK usage |
| **Dependencies** | `agent-framework`, `agent-framework-azure-ai` | `azure-ai-projects`, `azure-ai-agents`, `azure-ai-inference` |
| **Agent Creation** | `ChatAgent` with `AzureAIAgentClient` | Direct `AgentsClient.create_agent()` |
| **Streaming** | `agent.run_stream()` | `create_run_stream()` with event handling |
| **Middleware** | Built-in middleware decorators | Custom implementation with timing/logging |
| **Threading** | Thread serialization/deserialization | Thread persistence via thread ID |

## Prerequisites

1. **Azure AI Foundry Project** with deployed models
2. **Python 3.11+**
3. **Azure CLI** (for authentication)
4. Environment variables configured in `.env`

## Setup

1. Copy the environment template:
   ```bash
   cp .env.copy .env
   ```

2. Edit `.env` and configure your Azure resources:
   ```
   PROJECT_ENDPOINT=https://your-project.services.ai.azure.com/api/projects/your-project
   MODEL_DEPLOYMENT=gpt-4o-mini
   AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
   AZURE_OPENAI_CHAT_DEPLOYMENT_NAME=gpt-4o
   AZURE_OPENAI_API_KEY=your-key
   ```

3. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```
   
   Or using uv:
   ```bash
   uv pip install -r requirements.txt
   ```

4. Authenticate with Azure CLI:
   ```bash
   az login
   ```

## Demo Files

| Filename | Description | Migration Notes |
|----------|-------------|-----------------|
| `openai_chat.py` | Direct Azure OpenAI chat without Agent Service | Uses `azure-ai-inference` ChatCompletionsClient |
| `create_agent.py` | Create a new Azure AI Foundry Agent and chat | Uses `AgentsClient.create_agent()` |
| `streaming.py` | Demonstrate response streaming | Event-based streaming with `create_run_stream()` |
| `use_existing_agent.py` | Connect to an existing Azure AI Foundry Agent | Uses `AgentsClient.get_agent()` |
| `threading.py` | Thread management and persistence | Thread ID-based persistence |
| `chat_history.py` | Chat history management | Uses `list_messages()` for history |
| `long_term_memory.py` | AI-powered long-term memory | Custom memory implementation with file storage |
| `function_calling.py` | Function calling with timing and logging | Tool definitions and manual function execution |
| `observability.py` | OpenTelemetry observability | Custom span creation and tracking |
| `structured_output.py` | Extract structured data using Pydantic | JSON schema with response_format |
| `multimodal.py` | Process images with vision capabilities | File upload and attachments |

## Running the Demos

Each demo is a standalone Python script:

```bash
# Basic chat with Azure OpenAI
python openai_chat.py

# Create and interact with an agent
python create_agent.py

# Streaming responses
python streaming.py

# Thread persistence
python threading.py

# Function calling
python function_calling.py

# And so on...
```

## Utility Scripts

| Filename | Description |
|----------|-------------|
| `agents-delete-all.py` | Delete all agents in the project |

## Migration Guide

### From Agent Framework to Azure AI Foundry SDK

**Before (Agent Framework):**
```python
from agent_framework import ChatAgent
from agent_framework.azure import AzureAIAgentClient

agent = ChatAgent(
    chat_client=AzureAIAgentClient(
        project_client=project_client,
        agent_id=agent_id,
        async_credential=credential
    )
)

async for chunk in agent.run_stream(user_input):
    if chunk.text:
        print(chunk.text, end="")
```

**After (Azure AI Foundry SDK):**
```python
from azure.ai.agents.aio import AgentsClient

async with AgentsClient(endpoint=endpoint, credential=credential) as agents_client:
    agent = await agents_client.create_agent(
        model=model,
        name="My Agent",
        instructions="You are helpful."
    )
    
    thread = await agents_client.create_thread()
    await agents_client.create_message(thread_id=thread.id, role="user", content=user_input)
    
    async with await agents_client.create_run_stream(thread_id=thread.id, assistant_id=agent.id) as stream:
        async for event in stream:
            if event.event == "thread.message.delta":
                if hasattr(event.data, 'delta'):
                    for content in event.data.delta.content:
                        if hasattr(content, 'text'):
                            print(content.text.value, end="")
```

### Key Changes

1. **Agent Creation**: Direct `create_agent()` instead of `ChatAgent` wrapper
2. **Threading**: Explicit thread creation and management with `create_thread()`
3. **Messages**: Create messages in threads with `create_message()`
4. **Streaming**: Event-based streaming with specific event type handling
5. **Function Calling**: Manual tool output submission with `submit_tool_outputs()`
6. **Cleanup**: Explicit `delete_agent()` calls

## Architecture

```
┌─────────────────────────────────────────────┐
│         Your Application                     │
├─────────────────────────────────────────────┤
│      Azure AI Foundry SDK                    │
│  ┌────────────────────────────────────────┐ │
│  │ azure-ai-projects  (Project Client)    │ │
│  ├────────────────────────────────────────┤ │
│  │ azure-ai-agents    (Agent Service)     │ │
│  ├────────────────────────────────────────┤ │
│  │ azure-ai-inference (Chat Completions)  │ │
│  └────────────────────────────────────────┘ │
├─────────────────────────────────────────────┤
│         Azure AI Foundry                     │
│  ┌────────────┐  ┌──────────────────────┐  │
│  │   Agents   │  │  Azure OpenAI        │  │
│  └────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────┘
```

## Benefits of Direct SDK Usage

1. **Direct Control**: Full access to Azure AI capabilities without abstraction overhead
2. **Official Support**: Uses official Microsoft SDKs with full documentation
3. **Flexibility**: Easy to customize behavior and add features
4. **Transparency**: Clear understanding of what happens under the hood
5. **Performance**: Fewer layers between your code and Azure services

## Limitations Compared to Agent Framework

1. **More Verbose**: Requires more boilerplate code
2. **Manual Management**: Thread and message management is manual
3. **No Built-in Middleware**: Must implement custom middleware patterns
4. **Event Handling**: Streaming requires event-based parsing
5. **Cleanup**: Must manually delete agents and threads

## Best Practices

1. **Always use context managers** (`async with`) for proper resource cleanup
2. **Delete agents** when done to avoid resource accumulation
3. **Store thread IDs** for conversation persistence
4. **Handle streaming events** by checking event types
5. **Implement proper error handling** for network and API errors
6. **Use environment variables** for configuration
7. **Authenticate with DefaultAzureCredential** for production

## Troubleshooting

### Authentication Errors
- Ensure `az login` is completed
- Check that your account has access to the Azure AI Foundry project
- Verify `PROJECT_ENDPOINT` is correct

### Agent Creation Fails
- Check that `MODEL_DEPLOYMENT` matches a deployed model
- Verify the model supports the requested features (e.g., vision for multimodal)

### Streaming Issues
- Ensure you're handling the correct event types
- Check that attribute access uses `hasattr()` to avoid errors

### Thread Not Found
- Threads may be deleted or expired
- Always create a new thread if loading fails

## Additional Resources

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Agents SDK Reference](https://learn.microsoft.com/python/api/azure-ai-agents/)
- [Azure AI Projects SDK Reference](https://learn.microsoft.com/python/api/azure-ai-projects/)
- [Azure AI Inference SDK Reference](https://learn.microsoft.com/python/api/azure-ai-inference/)

## Related Demos

- Original Agent Framework demos: `../agentfw_basics-py/`
- Azure AI Foundry basics: `../../../01-essentials/01-foundry/02-sdks/foundry-sdk-py/`
- Agent Service demos: `../../demos/02-agent-service/`
