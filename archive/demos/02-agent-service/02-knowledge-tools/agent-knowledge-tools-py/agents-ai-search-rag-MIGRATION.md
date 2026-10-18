# Migration Guide: agents-ai-search-rag.py

## ⚠️ MIGRATION BLOCKED - But Alternative Available

**Current Status:** This sample **CANNOT be directly migrated** to the new Microsoft Foundry API.

**Blocker:** The `AzureAISearchTool` is not available in `azure.ai.projects.models`.

**Alternative:** Migrate to **Foundry IQ (Knowledge Base)** - a more powerful RAG solution using the new API.

---

## Current Implementation (Legacy API)

This sample uses:

- **API:** `azure.ai.agents.AgentsClient`
- **Tool:** `AzureAISearchTool`
- **Pattern:** Thread/Run with `create_and_process()`
- **RAG Approach:** Direct index search with simple query

```python
from azure.ai.agents.models import AzureAISearchTool, AzureAISearchQueryType

conn_id = project_client.connections.get(connection_name).id

ai_search = AzureAISearchTool(
    index_connection_id=conn_id,
    index_name=index_name,
    query_type=AzureAISearchQueryType.SIMPLE,
    top_k=3,
    filter="",
)

agent = agents_client.create_agent(
    model=model,
    name="insurance-rag-agent",
    instructions="...",
    tools=ai_search.definitions,
    tool_resources=ai_search.resources
)
```

---

## Recommended Migration: Foundry IQ

**Foundry IQ** is the modern alternative to Azure AI Search RAG, available in the new API.

### Benefits

- ✅ Works with new `AIProjectClient` API
- ✅ Orchestrated query planning and decomposition
- ✅ Better semantic understanding
- ✅ Automatic citation formatting
- ✅ Multi-source knowledge integration
- ✅ Shows up in Microsoft Foundry portal

### Prerequisites

1. **Azure AI Search Service** with semantic ranker enabled
2. **Knowledge Base** created in Azure AI Search (API `2025-11-01-preview`)
3. **Knowledge Sources** added to the knowledge base
4. **Project Connection** with `ProjectManagedIdentity` auth type
5. **Managed Identity** with `Search Index Data Reader` role

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
knowledge_base_name = "insurance-knowledge-base"  # Your KB name
mcp_endpoint = f"{search_endpoint}/knowledgebases/{knowledge_base_name}/mcp?api-version=2025-11-01-preview"

# Optimized instructions for knowledge base retrieval
instructions = """
You are a helpful assistant that must use the knowledge base to answer all questions about insurance products.
You must never answer from your own knowledge under any circumstances.
Every answer must always provide annotations for using the MCP knowledge base tool and render them as: 【message_idx:search_idx†source_name】
If you cannot find the answer in the provided knowledge base you must respond with "I don't know".
"""

# Create MCP tool for knowledge base
mcp_kb_tool = MCPTool(
    server_label="knowledge-base",
    server_url=mcp_endpoint,
    require_approval="never",
    allowed_tools=["knowledge_base_retrieve"],
    project_connection_id="foundry-iq-connection"
)

# Create agent with knowledge base tool
agent = project_client.agents.create_version(
    agent_name="insurance-rag-agent",
    definition=PromptAgentDefinition(
        model=model,
        instructions=instructions,
        tools=[mcp_kb_tool]
    )
)

# Use streaming responses
user_input = "Which policies cover a broken car side mirror?"
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

### Setup Guide

See `foundry-iq-todo.md` and `agents-foundry-iq.py` for complete Foundry IQ setup instructions.

---

## If You Must Keep Azure AI Search Tool

If you cannot migrate to Foundry IQ, keep this sample using the legacy API with this header note:

```python
# NOTE: This sample uses the legacy AgentsClient API because AzureAISearchTool
# is not yet available in the new Microsoft Foundry API (AIProjectClient).
# For RAG with the new API, see agents-foundry-iq.py (Foundry IQ Knowledge Base).
# See agents-ai-search-rag-MIGRATION.md for migration guidance.
```

---

**Last Updated:** November 27, 2025
