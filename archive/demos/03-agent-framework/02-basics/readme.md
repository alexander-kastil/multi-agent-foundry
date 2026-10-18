# Microsoft Agent Framework Basics & Concepts

- Introduction to the Agent Framework
- Chat Clients vs Agents: Key differences
- Agent types and configuration essentials
- Integrating Azure AI Foundry agents
- Threads, conversation persistence, and ChatReducer
- Managing agent memory
- Middleware & Observability
- Multi-modal capabilities and structured outputs

## Demo Files

| Filename                        | Description                                                                          |
| ------------------------------- | ------------------------------------------------------------------------------------ |
| `agentfw_openai_chat.py`        | Direct Azure OpenAI chat without Agent Service (non-persistent)                      |
| `agentfw_create_agent.py`       | Create a new Azure AI Foundry Agent and chat with it interactively                   |
| `agentfw_use_existing_agent.py` | Connect to an existing Azure AI Foundry Agent by ID                                  |
| `agentfw_file_search_tool.py`   | Use the File Search Tool to query documents in a vector store                        |
| `agentfw_threading_auto.py`     | Thread serialization and deserialization with automatic save/restore                 |
| `agentfw_long_term_memory.py`   | AI-powered long-term memory with intelligent context extraction                      |
| `agentfw_middleware.py`         | Complete middleware demo with timing, security, function logging, and token counting |
| `agentfw_observability.py`      | OpenTelemetry observability with comprehensive span data collection                  |
| `agentfw_multimodal.py`         | Process PDF documents with vision capabilities and extract structured invoice data   |
| `agentfw_structured_output.py`  | Extract structured data using Pydantic models                                        |
| `agentfw_streaming.py`          | Demonstrate response streaming for real-time token-by-token output                   |
