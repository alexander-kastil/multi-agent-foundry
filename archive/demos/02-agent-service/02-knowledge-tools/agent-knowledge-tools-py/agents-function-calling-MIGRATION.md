# Migration Guide: agents-function-calling.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `FunctionTool` and `ToolSet` are not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Function Calling tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `FunctionTool` with `ToolSet`
- **Pattern:** Thread/Run with automatic function execution
- **Capabilities:** Call user-defined Python functions

```python
from azure.ai.agents.models import FunctionTool, ToolSet

def get_user_email() -> str:
    """Get the user's email address."""
    return input("Please enter your email address: ")

def submit_support_ticket(email: str, issue: str) -> str:
    """Submit a support ticket with the user's email and issue description."""
    # Implementation...
    return json.dumps({"success": True, "ticket_id": ticket_id})

# Create function tool
functions = FunctionTool([get_user_email, get_issue_description, submit_support_ticket])
toolset = ToolSet()
toolset.add(functions)

# Enable automatic function calling
agents_client.enable_auto_function_calls(toolset)

# Create agent
agent = agents_client.create_agent(
    model=model,
    name="support-agent",
    instructions="You are a technical support agent...",
    toolset=toolset
)
```

---

## Possible Workaround: MCP Server Wrapper

While there's no direct Function Calling in the new API, you could potentially wrap your functions in a **Model Context Protocol (MCP) server** as a workaround.

### Conceptual Approach

1. Create an MCP server that exposes your functions as MCP tools
2. Deploy the MCP server (locally or remotely)
3. Use `MCPTool` in the new API to connect to your server

This is significantly more complex than the current approach and may not be worth the effort for simple function calling.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because FunctionTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-function-calling-MIGRATION.md for details.
```

---

## Minor Updates (Current Implementation)

While waiting for tool support:

1. **Update environment variable convention:**

```python
# Before:
delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"

# After:
delete_resources = os.getenv("DELETE", "true").lower() == "true"
```

2. **Update comments:**

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
- Function calling support in `PromptAgentDefinition`
- Documentation on custom function integration

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, FunctionTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

# Define functions (same as before)
def submit_support_ticket(email: str, issue: str) -> str:
    # Implementation...
    pass

# Create agent with functions
agent = project_client.agents.create_version(
    agent_name="support-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a technical support agent...",
        tools=[FunctionTool([submit_support_ticket])]  # When available
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="I need help with my account",
    stream=True,
    extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
)

for event in response:
    if event.type == "response.output_text.delta":
        print(event.delta, end='', flush=True)
    elif event.type == "response.completed":
        print()
        break
```

---

**Last Updated:** November 27, 2025
