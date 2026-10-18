# Migration Guide: agents-bing-grounding.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `BingGroundingTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Bing Grounding tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `BingGroundingTool`
- **Pattern:** Thread/Run with `create_and_process()`
- **Connection:** Bing connection from project connections

```python
from azure.ai.agents.models import BingGroundingTool

conn_id = project_client.connections.get(bing_connection_name).id

bing = BingGroundingTool(connection_id=conn_id)

agent = agents_client.create_agent(
    model=model,
    name="bing-grounding-agent",
    instructions="You are a helpful agent",
    tools=bing.definitions
)
```

---

## No Direct Alternative in New API

Unlike Azure AI Search (which can be replaced with Foundry IQ), there is currently **no alternative** to Bing Grounding in the new API.

Bing Grounding provides:

- Real-time web search capabilities
- Current information retrieval
- Citation support with URLs
- Fact-checking against live web data

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because BingGroundingTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-bing-grounding-MIGRATION.md for details.
```

---

## Minor Updates (Current Implementation)

While waiting for tool support, you can make these improvements:

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

- Addition of `BingGroundingTool` in `azure.ai.projects.models`
- Release notes indicating web search/grounding support
- Documentation updates

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, BingGroundingTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

# Get connection (same as before)
conn_id = project_client.connections.get(bing_connection_name).id

# Create agent with Bing tool
agent = project_client.agents.create_version(
    agent_name="bing-grounding-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a helpful agent",
        tools=[BingGroundingTool(connection_id=conn_id)]  # When available
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="How does wikipedia explain Euler's Identity?",
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
