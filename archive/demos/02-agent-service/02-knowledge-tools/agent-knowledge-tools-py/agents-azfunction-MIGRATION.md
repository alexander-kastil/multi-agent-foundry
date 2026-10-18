# Migration Guide: agents-azfunction.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `FunctionTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Function Tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `FunctionTool` with custom functions
- **Pattern:** Thread/Run with automatic function execution
- **External Service:** Azure Functions for currency conversion

```python
from azure.ai.agents.models import FunctionTool, ToolSet

def convert_currency_via_function(from_currency: str, to_currency: str, amount: float, date: Optional[str] = None) -> str:
    """Call external Azure Function for currency conversion."""
    FUNCTION_DEPLOYMENT_URL = os.getenv("FUNCTION_DEPLOYMENT_URL")
    response = requests.post(FUNCTION_DEPLOYMENT_URL, json=payload, timeout=20)
    return json.dumps(response.json())

# Create function tool
functions = FunctionTool([convert_currency_via_function])
toolset = ToolSet()
toolset.add(functions)

# Enable automatic function calling
agents_client.enable_auto_function_calls(toolset)

# Create agent
agent = agents_client.create_agent(
    model=model,
    name="currency-conversion-agent",
    instructions="You assist with currency conversion questions...",
    toolset=toolset
)
```

---

## Possible Workaround: MCP Server

You could wrap the Azure Function as an MCP server, which would work with the new API:

### Conceptual Approach

1. Create an MCP server that acts as a proxy to your Azure Function
2. Deploy the MCP server
3. Use `MCPTool` to connect to it

```python
from azure.ai.projects.models import MCPTool

# MCP server that wraps your Azure Function
mcp_tool = MCPTool(
    server_label="currency-converter",
    server_url="https://your-mcp-server.azurewebsites.net",
    allowed_tools=["convert_currency"],
    require_approval="never"
)

agent = project_client.agents.create_version(
    agent_name="currency-conversion-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You assist with currency conversion questions...",
        tools=[mcp_tool]
    )
)
```

This requires significant additional infrastructure and may not be worth the effort.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because FunctionTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-azfunction-MIGRATION.md for details.
```

---

## Minor Updates (Current Implementation)

While waiting for tool support:

1. **Remove UTF-8 encoding handling:**

```python
# Remove these lines:
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
```

2. **Update environment variable convention:**

```python
# Before:
delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"

# After:
delete_resources = os.getenv("DELETE", "true").lower() == "true"
```

3. **Update comments:**

```python
# Before:
print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")

# After:
print(f"Agent {agent.id} preserved for examination in Microsoft Foundry")
```

---

## When to Migrate

Monitor the `azure-ai-projects` package for:

- Addition of `FunctionTool` in `azure.ai.projects.models`
- Support for external API calls
- Documentation on integrating Azure Functions

---

**Last Updated:** November 27, 2025
