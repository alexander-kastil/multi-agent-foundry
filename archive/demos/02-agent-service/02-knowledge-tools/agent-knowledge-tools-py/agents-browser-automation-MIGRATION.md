# Migration Guide: agents-browser-automation.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `BrowserAutomationTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Browser Automation tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `BrowserAutomationTool` (Playwright-based)
- **Pattern:** Thread/Run with `create_and_process()`
- **Connection:** Azure Playwright connection for browser automation

```python
from azure.ai.agents.models import BrowserAutomationTool

connection_id = os.getenv("AZURE_PLAYWRIGHT_CONNECTION_ID")

browser_automation = BrowserAutomationTool(connection_id=connection_id)

agent = agents_client.create_agent(
    model=model,
    name="browser-automation-agent",
    instructions="You are a helpful agent that can automate web browser tasks...",
    tools=browser_automation.definitions
)
```

---

## No Alternative in New API

Browser Automation provides unique capabilities:

- Navigate to URLs and interact with web pages
- Extract information from websites
- Fill forms and click buttons
- Take screenshots
- Automate web workflows

There is currently **no equivalent** in the new API.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because BrowserAutomationTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-browser-automation-MIGRATION.md for details.
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

- Addition of `BrowserAutomationTool` in `azure.ai.projects.models`
- Playwright/browser integration support
- Documentation on web automation capabilities

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, BrowserAutomationTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

connection_id = os.getenv("AZURE_PLAYWRIGHT_CONNECTION_ID")

agent = project_client.agents.create_version(
    agent_name="browser-automation-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a helpful agent that can automate web browser tasks...",
        tools=[BrowserAutomationTool(connection_id=connection_id)]  # When available
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="Navigate to onvista.de and get the gold price",
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
