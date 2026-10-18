# Mastering Tools & Knowledge

- Built-in tools: Code Interpreter, File Search, Web Search
- Using vector stores for memory and retrieval
- Adding custom tools and calling them from agents
- Integrating OpenAPI and MCP tools
- Human-in-the-loop strategies for tool calls
- Function-calling middleware for advanced workflows

## Demo Files

| Filename                              | Description                                                                   |
| ------------------------------------- | ----------------------------------------------------------------------------- |
| `agentfw_builtin_tools.py`            | Demonstrates built-in Code Interpreter and Web Search tools (educational)     |
| `agentfw_bing_search.py`              | Working Bing web search using Azure AI Agents Service                         |
| `agentfw_file_search_tool.py`         | Use the built-in File Search Tool to query documents in a vector store        |
| `agentfw_function_tool_calculator.py` | Create and use a custom calculator function tool with Agent Framework         |
| `agentfw_multiple_tools.py`           | Demonstrate multiple custom tools (weather, time, currency) working together  |
| `agentfw_rest_api_tool.py`            | Create custom tools that call external REST APIs (Food Catalog API)           |
| `agentfw_mcp_local.py`                | Connect to a local MCP calculator server for math operations                  |
| `agentfw_mcp_external.py`             | Connect to an external MCP server (weather example)                           |
| `agentfw_human_in_the_loop.py`        | Human-in-the-loop approval system for dangerous operations like file deletion |
