# Migration Guide: agents-mcp.py

## ✅ MIGRATION READY - Can Be Migrated

**Current Status:** This sample **CAN be migrated** to the new Microsoft Foundry API with minor changes.

**Changes Required:** Update import statements and capitalize `MCPTool` (was `McpTool`).

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `McpTool` (lowercase 'c')
- **Pattern:** Thread/Run with manual tool approval
- **MCP Server:** GitHub MCP server for Azure REST API specs

```python
from azure.ai.agents.models import McpTool  # lowercase 'c'

mcp_tool = McpTool(
    server_label="github",
    server_url="https://gitmcp.io/Azure/azure-rest-api-specs",
    allowed_tools=["search_azure_rest_api_code"]
)

agent = agents_client.create_agent(
    model=model,
    name="mcp-agent",
    instructions="You are a helpful agent that can use MCP tools...",
    tools=mcp_tool.definitions
)
```

---

## Migration to New API

### Step 1: Update Imports

**Before:**

```python
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import McpTool, ListSortOrder, RequiredMcpToolCall, SubmitToolApprovalAction, ToolApproval
```

**After:**

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, MCPTool  # Capitalized
```

### Step 2: Update Client Initialization

**Before:**

```python
agents_client = AgentsClient(
    endpoint=endpoint,
    credential=DefaultAzureCredential()
)
```

**After:**

```python
project_client = AIProjectClient(
    endpoint=endpoint,
    credential=DefaultAzureCredential()
)
openai_client = project_client.get_openai_client()
```

### Step 3: Update MCP Tool Creation

**Before:**

```python
mcp_tool = McpTool(  # lowercase 'c'
    server_label="github",
    server_url="https://gitmcp.io/Azure/azure-rest-api-specs",
    allowed_tools=[]
)
mcp_tool.allow_tool("search_azure_rest_api_code")
```

**After:**

```python
mcp_tool = MCPTool(  # uppercase 'CP'
    server_label="github",
    server_url="https://gitmcp.io/Azure/azure-rest-api-specs",
    allowed_tools=["search_azure_rest_api_code"],
    require_approval="never"  # New parameter
)
```

### Step 4: Update Agent Creation

**Before:**

```python
agent = agents_client.create_agent(
    model=model,
    name="mcp-agent",
    instructions="You are a helpful agent that can use MCP tools...",
    tools=mcp_tool.definitions
)
```

**After:**

```python
agent = project_client.agents.create_version(
    agent_name="mcp-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a helpful agent that can use MCP tools...",
        tools=[mcp_tool]
    )
)
```

### Step 5: Replace Thread/Run with Streaming Responses

**Before:**

```python
thread = agents_client.threads.create()
message = agents_client.messages.create(
    thread_id=thread.id,
    role="user",
    content="Please summarize the Azure REST API specifications Readme"
)

# Manual tool approval loop
run = agents_client.runs.create(thread_id=thread.id, agent_id=agent.id)
while run.status in ["queued", "in_progress", "requires_action"]:
    if run.status == "requires_action":
        # Handle tool approvals...
        pass
    time.sleep(1)
    run = agents_client.runs.get(thread_id=thread.id, run_id=run.id)

messages = agents_client.messages.list(thread_id=thread.id)
```

**After:**

```python
response = openai_client.responses.create(
    input="Please summarize the Azure REST API specifications Readme",
    stream=True,
    extra_body={
        "agent": {
            "type": "agent_reference",
            "name": agent.name,
            "version": agent.version
        }
    }
)

print("agent: ", end='', flush=True)
for event in response:
    if event.type == "response.output_text.delta":
        print(event.delta, end='', flush=True)
    elif event.type == "response.completed":
        print()
        break
```

### Step 6: Update Cleanup

**Before:**

```python
delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
if delete_on_exit:
    agents_client.delete_agent(agent.id)
else:
    print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")
```

**After:**

```python
delete_resources = os.getenv("DELETE", "true").lower() == "true"
if delete_resources:
    project_client.agents.delete_version(
        agent_name=agent.name,
        agent_version=agent.version
    )
else:
    print(f"Agent {agent.name}:{agent.version} preserved for examination in Microsoft Foundry")
```

---

## Complete Migrated Code

```python
import os
import time
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, MCPTool
from azure.identity import DefaultAzureCredential

# Demonstrates Model Context Protocol (MCP) integration using Microsoft Foundry API.
# The agent uses MCP tools to access external services (e.g., GitHub API specs).

def main():
    os.system('cls' if os.name == 'nt' else 'clear')
    load_dotenv()

    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    mcp_server_url = os.getenv("MCP_SERVER_URL", "https://gitmcp.io/Azure/azure-rest-api-specs")
    mcp_server_label = os.getenv("MCP_SERVER_LABEL", "github")

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"MCP Server: {mcp_server_label} at {mcp_server_url}")

    project_client = AIProjectClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential()
    )
    openai_client = project_client.get_openai_client()

    with project_client:
        start = time.time()

        # Create MCP tool
        mcp_tool = MCPTool(
            server_label=mcp_server_label,
            server_url=mcp_server_url,
            allowed_tools=["search_azure_rest_api_code"],
            require_approval="never"
        )

        # Create versioned agent
        agent = project_client.agents.create_version(
            agent_name="mcp-agent",
            definition=PromptAgentDefinition(
                model=model,
                instructions="You are a helpful agent that can use MCP tools to assist users.",
                tools=[mcp_tool]
            )
        )
        print(f"Created agent: {agent.name} (version {agent.version})")

        # Create streaming response
        user_input = "Please summarize the Azure REST API specifications Readme"
        response = openai_client.responses.create(
            input=user_input,
            stream=True,
            extra_body={
                "agent": {
                    "type": "agent_reference",
                    "name": agent.name,
                    "version": agent.version
                }
            }
        )

        print("agent: ", end='', flush=True)
        for event in response:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()
                duration = time.time() - start
                print(f"Response completed (took {duration:.2f}s)")
                break

        # Cleanup
        delete_resources = os.getenv("DELETE", "true").lower() == "true"
        if delete_resources:
            project_client.agents.delete_version(
                agent_name=agent.name,
                agent_version=agent.version
            )
            print("Deleted agent version")
        else:
            print(f"Preserved agent: {agent.name}:{agent.version}")


if __name__ == '__main__':
    main()
```

---

## Environment Variable Updates

Update `.env` file:

**Before:**

```env
DELETE_AGENT_ON_EXIT=true
```

**After:**

```env
DELETE=true
```

---

## Key Changes Summary

1. ✅ Import: `McpTool` → `MCPTool` (capitalized)
2. ✅ Client: `AgentsClient` → `AIProjectClient`
3. ✅ Agent creation: `create_agent()` → `create_version()` with `PromptAgentDefinition`
4. ✅ Conversation: Thread/Run → Streaming responses
5. ✅ Cleanup: `delete_agent()` → `delete_version()`
6. ✅ Env var: `DELETE_AGENT_ON_EXIT` → `DELETE`
7. ✅ Remove UTF-8 encoding handling (not needed)

---

## Benefits of Migration

- ✅ Versioned agents (appear in Microsoft Foundry portal)
- ✅ Real-time streaming responses (better UX)
- ✅ Simplified conversation flow
- ✅ Better resource management

---

**Last Updated:** November 27, 2025
