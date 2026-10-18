# Migration Guide: agents-code-interpreter.py

## ⚠️ MIGRATION BLOCKED - No Upgrade Path Available

**Current Status:** This sample **CANNOT be migrated** to the new Microsoft Foundry API at this time.

**Blocker:** The `CodeInterpreterTool` is not available in `azure.ai.projects.models`. The new API currently only supports `MCPTool`.

**Recommendation:** Keep this sample using the legacy `AgentsClient` API until Microsoft adds Code Interpreter tool support to the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `CodeInterpreterTool`
- **Pattern:** Thread/Run with `create_and_process()`
- **Capabilities:** Execute Python code, analyze files, generate charts

```python
from azure.ai.agents.models import CodeInterpreterTool, FilePurpose

# Upload file
file = agents_client.files.upload_and_poll(
    file_path=asset_file_path,
    purpose=FilePurpose.AGENTS
)

# Create code interpreter tool
code_interpreter = CodeInterpreterTool(file_ids=[file.id])

# Create agent
agent = agents_client.create_agent(
    model=model,
    name="code-interpreter-agent",
    instructions="You are a helpful agent with access to code interpreter tools...",
    tools=code_interpreter.definitions,
    tool_resources=code_interpreter.resources
)
```

---

## No Alternative in New API

Code Interpreter provides unique capabilities:

- Execute Python code in a sandboxed environment
- Analyze uploaded CSV/Excel files
- Generate data visualizations (charts, graphs)
- Perform calculations and data transformations
- Create and download result files

There is currently **no equivalent** in the new API.

---

## Recommended File Header Update

Add this note at the top of the file:

```python
# NOTE: This sample uses the legacy AgentsClient API because CodeInterpreterTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# See agents-code-interpreter-MIGRATION.md for details.
```

---

## Minor Updates (Current Implementation)

While waiting for tool support:

1. **Remove UTF-8 encoding handling:**

```python
# Remove these lines:
if sys.platform == 'win32':
    import io
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

- Addition of `CodeInterpreterTool` in `azure.ai.projects.models`
- File upload/management support in new API
- Sandbox execution capabilities

Once available, migration pattern would be:

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, CodeInterpreterTool  # When available

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

# Upload file (API to be determined)
# file = project_client.files.upload(...)

# Create agent with code interpreter
agent = project_client.agents.create_version(
    agent_name="code-interpreter-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are a helpful agent with access to code interpreter tools...",
        tools=[CodeInterpreterTool(file_ids=[file.id])]  # When available
    )
)

# Use streaming responses
response = openai_client.responses.create(
    input="Create a bar chart for operating profit in TRANSPORTATION sector",
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
