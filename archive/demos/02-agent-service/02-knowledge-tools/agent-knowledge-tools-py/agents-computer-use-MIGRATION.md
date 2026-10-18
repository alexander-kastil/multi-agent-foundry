# Migration Guide: agents-computer-use.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `ComputerUseTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Computer Use tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `ComputerUseTool`
- **Pattern:** Thread/Run with manual tool approval
- **Capabilities:** Desktop automation (screenshots, mouse, keyboard)

```python
from azure.ai.agents.models import ComputerUseTool

# Initialize Computer Use tool with viewport
computer_use = ComputerUseTool(
    display_width=1026,
    display_height=769,
    environment=environment
)

agent = agents_client.create_agent(
    model=model,
    name="agent-computer-use",
    instructions="You are an computer automation assistant...",
    tools=computer_use.definitions
)
```

---

## No Alternative in New API

Computer Use provides unique capabilities:

- Take screenshots of desktop applications
- Simulate mouse clicks and movements
- Type text via keyboard simulation
- Interact with GUI applications
- Automate desktop workflows

There is currently **no equivalent** in the new API.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because ComputerUseTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-computer-use-MIGRATION.md for details.
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

- Addition of `ComputerUseTool` in `azure.ai.projects.models`
- Desktop automation support
- Documentation on computer use capabilities

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, ComputerUseTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

environment = os.getenv("COMPUTER_USE_ENVIRONMENT")

agent = project_client.agents.create_version(
    agent_name="agent-computer-use",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are an computer automation assistant...",
        tools=[ComputerUseTool(display_width=1026, display_height=769, environment=environment)]
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="Type 'movies near me' in the search box",
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
