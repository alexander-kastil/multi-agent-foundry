# Migration Guide: agents-sharepoint.py

## ⚠️ MIGRATION BLOCKED - But Alternative Available

**Current Status:** This sample **CANNOT be directly migrated** to the new Microsoft Foundry API.

**Blocker:** The `SharepointTool` is not available in `azure.ai.projects.models`.

**Alternative:** Use **Foundry IQ (Knowledge Base)** with a SharePoint knowledge source.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `SharepointTool`
- **Pattern:** Thread/Run with `create_and_process()`
- **Connection:** SharePoint connection from project connections

```python
from azure.ai.agents.models import SharepointTool

conn_id = project_client.connections.get(sharepoint_connection_name).id

sharepoint = SharepointTool(connection_id=conn_id)

agent = agents_client.create_agent(
    model=model,
    name="sharepoint-agent",
    instructions="You are a SharePoint knowledge assistant...",
    tools=sharepoint.definitions
)
```

---

## Alternative: Foundry IQ with SharePoint Knowledge Source

**Foundry IQ** supports SharePoint as a knowledge source, providing similar capabilities through the new API.

### Benefits of Foundry IQ Approach

- ✅ Works with new `AIProjectClient` API
- ✅ Better semantic search across SharePoint content
- ✅ Query orchestration and planning
- ✅ Multi-source support (combine SharePoint with other sources)
- ✅ Automatic citation formatting
- ✅ Shows up in Microsoft Foundry portal

### Prerequisites

1. **Azure AI Search Service** with semantic ranker enabled
2. **SharePoint Connection** in your project
3. **Knowledge Base** created in Azure AI Search
4. **SharePoint Knowledge Source** added to the knowledge base
5. **Project Connection** for MCP with `ProjectManagedIdentity` auth
6. **Managed Identity** with `Search Index Data Reader` role

### Migration Code

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, MCPTool

project_client = AIProjectClient(endpoint=endpoint, credential=credential)
openai_client = project_client.get_openai_client()

# Get search endpoint from connection
search_conn = project_client.connections.get("procodeaisearch")
search_endpoint = search_conn.properties.get("target")

# Construct MCP endpoint for knowledge base
knowledge_base_name = "sharepoint-knowledge-base"  # Your KB name
mcp_endpoint = f"{search_endpoint}/knowledgebases/{knowledge_base_name}/mcp?api-version=2025-11-01-preview"

# Optimized instructions for SharePoint knowledge retrieval
instructions = """
You are a SharePoint knowledge assistant.
You MUST search the knowledge base for relevant SharePoint documents before answering any question.
Always provide annotations for using the MCP knowledge base tool and render them as: 【message_idx:search_idx†source_name】
Include citations from the SharePoint documents in your responses.
Do not rely on general knowledge - only answer based on SharePoint content.
If you cannot find the answer in the provided knowledge base you must respond with "I don't know".
"""

# Create MCP tool for knowledge base
mcp_kb_tool = MCPTool(
    server_label="sharepoint-kb",
    server_url=mcp_endpoint,
    require_approval="never",
    allowed_tools=["knowledge_base_retrieve"],
    project_connection_id="foundry-iq-connection"
)

# Create agent with knowledge base tool
agent = project_client.agents.create_version(
    agent_name="sharepoint-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions=instructions,
        tools=[mcp_kb_tool]
    )
)

# Use streaming responses
user_input = "What kind of sightseeing can you recommend in Vienna?"
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
        break

# Cleanup
delete_resources = os.getenv("DELETE", "true").lower() == "true"
if delete_resources:
    project_client.agents.delete_version(
        agent_name=agent.name,
        agent_version=agent.version
    )
```

### Setting Up SharePoint Knowledge Source

In Azure AI Search, create a knowledge source that points to your SharePoint site:

- **Type:** SharePoint
- **Connection:** Your SharePoint connection
- **Site URL:** SharePoint site to index
- **Document Library:** Specific library or entire site
- **Indexing Schedule:** Configure refresh frequency

See `foundry-iq-todo.md` for complete setup instructions.

---

## If You Must Keep SharePoint Tool

If you cannot migrate to Foundry IQ, keep this sample using the legacy API with this header note:

```python
# NOTE: This sample uses the legacy AgentsClient API because SharepointTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# For SharePoint search with the new API, see Foundry IQ with SharePoint knowledge source.
# See agents-sharepoint-MIGRATION.md for migration guidance.
```

---

**Last Updated:** November 27, 2025
