# Migration Guide: agents-rest-calling.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `OpenApiTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds OpenAPI tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `OpenApiTool` with anonymous authentication
- **Pattern:** Thread/Run with `create_and_process()`
- **OpenAPI Spec:** Inline OpenAPI 3.0 specification for DummyJSON API

```python
from azure.ai.agents.models import OpenApiAnonymousAuthDetails, OpenApiTool

# Define OpenAPI specification
openapi_spec = {
    "openapi": "3.0.0",
    "info": {"title": "DummyJSON Todos API", "version": "1.0.0"},
    "servers": [{"url": rest_url}],
    "paths": {
        "/todos": {...},
        "/todos/{id}": {...}
    }
}

# Create OpenAPI tool
openapi_tool = OpenApiTool(
    name="DummyJSON_Todos_API",
    spec=openapi_spec,
    description="API for managing todos...",
    auth=OpenApiAnonymousAuthDetails()
)

agent = agents_client.create_agent(
    model=model,
    name="rest-calling-agent",
    instructions="You are a helpful agent that can call REST APIs...",
    tools=openapi_tool.definitions
)
```

---

## No Direct Alternative in New API

OpenAPI Tool provides:

- Automatic REST API integration from OpenAPI specs
- Schema validation
- Multiple authentication methods
- Dynamic endpoint discovery

There is currently **no equivalent** in the new API.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because OpenApiTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-rest-calling-MIGRATION.md for details.
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

---

## When to Migrate

Monitor the `azure-ai-projects` package for:

- Addition of `OpenApiTool` in `azure.ai.projects.models`
- REST API integration support
- OpenAPI specification handling

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, OpenApiTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

# Define OpenAPI spec (same as before)
openapi_spec = {...}

agent = project_client.agents.create_version(
    agent_name="rest-calling-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a helpful agent that can call REST APIs...",
        tools=[OpenApiTool(name="DummyJSON_Todos_API", spec=openapi_spec)]  # When available
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="Get all todos",
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
