# Build a Knowledge Base and Connect an Agent to It

An agent that answers from its own training data guesses about your catalog and your store policies. File search helps with one uploaded document, but real answers span several indexes: a product sheet says a tent is rated for winter, and a policy page says what to do when it arrives damaged. In this demo you index two document sets, wrap them in one knowledge base, watch it plan a two-hop question and let a Python agent and a C# agent use it over MCP.

You finish with a Foundry IQ knowledge base, a project connection and two agents that cite their sources and admit when they do not know.

## Prerequisites

- A Microsoft Foundry project with `gpt-5.4` and `text-embedding-3-small` deployments, and `az login` completed.
- An Azure AI Search service on the Basic tier or higher in a region that supports agentic retrieval, with role-based access enabled. `provision.azcli` creates one and assigns the roles.
- Owner on the Foundry resource and the search service, to create the role assignments.
- Python 3.12 or later; the .NET 10 SDK for the C# project.
- Foundry IQ and the 2026-08-01-preview API are in preview. The project pins `azure-search-documents` 12.1.0b2.

## Run it

```bash
cd demos/03-agent-service/04-foundry-iq
bash provision.azcli
cd foundry-iq-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python create_knowledge_base.py
.venv/Scripts/python retrieve.py
.venv/Scripts/python connect_agent.py
```

Edit the variables at the top of `provision.azcli` first, then paste the four values it prints into `.env`. The class runs on the `pro-code-agents` project; `FOUNDRY_ENDPOINT` is the `cognitiveservices.azure.com` host of the Foundry resource, because the search service identity was rejected with 401 on the `openai.azure.com` host of that resource while the other supported hosts accepted it. The search service in the module sits in West Europe because Sweden Central had no Basic capacity when it was created.

## Step 1: Index the documents

Two small document sets stand in for a product catalog and a policy library. Each index has a text field, a vector field filled with `text-embedding-3-small` embeddings, an Azure OpenAI vectorizer for query-time vectors and a semantic configuration, because the knowledge base reranks semantically.

```python
index_client.create_or_update_index(build_index(index_name))
SearchClient(endpoint=SEARCH_ENDPOINT, index_name=index_name, credential=credential).upload_documents(documents)
```

Expected output:

```text
== Step 1: index the documents
m3-products: 8 documents
m3-policies: 6 documents
```

## Step 2: Create knowledge sources and a knowledge base

A knowledge source points at an index. The knowledge base lists both sources, names the model that plans and writes, and carries retrieval instructions that tell it which source answers which kind of question.

```python
index_client.create_or_update_knowledge_base(
    KnowledgeBase(
        name=KNOWLEDGE_BASE,
        retrieval_instructions="Use m3-products-ks for questions about gear, specifications and prices. Use m3-policies-ks for returns, warranty, shipping and repairs. Use both when a question needs both.",
        answer_instructions="Answer briefly and cite the documents you used.",
        output_mode=KnowledgeRetrievalOutputMode.ANSWER_SYNTHESIS,
        knowledge_sources=[KnowledgeSourceReference(name=name) for name in SOURCES.values()],
        models=[KnowledgeBaseAzureOpenAIModel(azure_open_ai_parameters=AzureOpenAIVectorizerParameters(
            resource_url=FOUNDRY_ENDPOINT, deployment_name=MODEL, model_name=MODEL))],
        retrieval_reasoning_effort=KnowledgeRetrievalLowReasoningEffort(),
    )
)
```

Expected output:

```text
== Step 2: create knowledge sources and a knowledge base
knowledge source m3-products-ks -> index m3-products
knowledge source m3-policies-ks -> index m3-policies
knowledge base m3-outdoor-kb created with default reasoning effort low
```

If this step fails with an authorization error from the model, the search service identity is missing the Cognitive Services User role on the Foundry resource.

## Step 3: Compare reasoning effort on a two-hop question

The question needs the products source (which tent handles winter) and the policies source (what to do about damage). The script sends it three times with different effort and prints how much work each run did, using the activity log in the response.

```python
response = client.retrieve(
    KnowledgeBaseRetrievalRequest(retrieval_reasoning_effort=effort, include_activity=True, messages=messages, output_mode=...)
)
```

Expected output (timings and token counts vary):

```text
== Step 3: compare reasoning effort

-- minimal: 4.2s, 2 index searches, 3 references
   planning tokens: 0, synthesis tokens: 0
   [{"ref_id":0,"title":"Summit 4S Tent","content":"Four-season two-person mountaineering tent for winter camping. Price 389 USD. ...

-- low: 7.3s, 6 index searches, 8 references
   planning tokens: 1465, synthesis tokens: 2201
   For a January trip, the **Summit 4S** is the better choice for winter storms because it is a **four-season** tent made for **winter camping**,[ref_id:1] and it is rated to **-25 degrees Celsius** and **wind gusts up to 90 km/h**,[ref_id:1] ...

-- medium: 12.9s, 4 index searches, 6 references
   planning tokens: 1462, synthesis tokens: 2067
   For a January trip, the Summit 4S is the better choice for winter storms because it is a four-season mountaineering tent for winter camping,[ref_id:1] ...
```

Minimal returns raw extracted passages from both sources without any model call. Low and medium split the question into subqueries, search the sources and return a synthesized answer with `ref_id` citations. For a question this size the extra reasoning of medium buys little, and it takes almost twice as long as low in this run.

## Step 4: Create the project connection

The agent reaches the knowledge base through its MCP endpoint, and a project connection provides the credential. This is a `RemoteTool` connection that signs in with the project managed identity for the Azure AI Search audience.

```python
requests.put(
    f"https://management.azure.com{PROJECT_RESOURCE_ID}/connections/{CONNECTION}?api-version=2025-10-01-preview",
    headers={"Authorization": f"Bearer {management_token}"},
    json={
        "name": CONNECTION,
        "type": "Microsoft.MachineLearningServices/workspaces/connections",
        "properties": {
            "authType": "ProjectManagedIdentity",
            "category": "RemoteTool",
            "target": MCP_ENDPOINT,
            "isSharedToAll": True,
            "audience": "https://search.azure.com/",
            "metadata": {"ApiType": "Azure"},
        },
    },
)
```

Expected output:

```text
== Step 4: create the project connection
connection m3-outdoor-kb-mcp -> https://m3-procode-search.search.windows.net/knowledgebases/m3-outdoor-kb/mcp?api-version=2026-08-01-preview
```

## Step 5: Attach the knowledge base to an agent

The agent gets one MCP tool restricted to `knowledge_base_retrieve`. The instructions make the knowledge base mandatory and define the fallback answer, which is what keeps the agent from guessing.

```python
MCPTool(
    server_label="knowledge-base",
    server_url=MCP_ENDPOINT,
    require_approval="never",
    allowed_tools=["knowledge_base_retrieve"],
    project_connection_id=CONNECTION,
)
```

Expected output (answers vary in wording):

```text
== Step 5: attach the knowledge base to an agent as an MCP tool
Created m3-outdoor-agent:1

user: Which of your tents is rated for winter storms, and what does it cost?
items: mcp_list_tools, mcp_call, message
agent: The tent rated for winter storms is the Summit 4S Tent, and it costs 389 USD【5:0†source】【5:1†source】

user: If that tent arrives damaged, what must I do and how long do I have? Does the warranty cover seam failures?
items: mcp_call, message
agent: If the Summit 4S Tent arrives damaged in transit, you must report it within 48 hours of delivery and include photos【9:4†source】. For transit damage, the item is then replaced or refunded within 60 days【9:4†source】. ...

user: Do you sell snowshoes?
items: mcp_call, message
agent: I don't know.
```

The follow-up question says "that tent" and still lands on the policies source, because the conversation carries the earlier answer. The snowshoe question shows the guardrail: the knowledge base has nothing on snowshoes, so the agent says so.

## Step 6: Call the same knowledge base from C#

The knowledge base belongs to the search service, not to a language. The C# program builds the same MCP tool with `ResponseTool.CreateMcpTool`, sets the project connection and asks two questions.

```bash
cd demos/03-agent-service/04-foundry-iq/foundry-iq-cs
cp .env.example .env
dotnet run
```

```csharp
McpTool knowledgeBase = ResponseTool.CreateMcpTool(
    serverLabel: "knowledge-base",
    serverUri: new Uri(mcpEndpoint),
    allowedTools: new McpToolFilter { ToolNames = { "knowledge_base_retrieve" } },
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
knowledgeBase.ProjectConnectionId = Environment.GetEnvironmentVariable("KNOWLEDGE_BASE_CONNECTION")!;
```

Expected output:

```text
Created m3-outdoor-agent-cs:1

user: Which of your tents is rated for winter storms, and what does it cost?
items: McpToolDefinitionListItem, McpToolCallItem, InternalResponsesAssistantMessage
agent: The **Summit 4S Tent** is the tent rated for winter storms/winter conditions. It costs **389 USD**【5:0†source】【5:1†source】

user: If that tent arrives damaged, what must I do and how long do I have?
items: McpToolCallItem, InternalResponsesAssistantMessage
agent: You must **report the transit damage within 48 hours of delivery** and **provide photos**. ...
```

## Verify

Both agents answer from the indexed documents (389 USD, 48 hours, 60 days, five-year seam warranty), each answer carries source markers, and the snowshoe question returns "I don't know". The Step 3 numbers show minimal making no model calls while low and medium plan and synthesize.

## Cleanup

`connect_agent.py` and the C# program delete their agent versions and conversations. `cleanup.py` removes the knowledge base, both knowledge sources, both indexes and the project connection.

```bash
.venv/Scripts/python cleanup.py
```

Expected output:

```text
== Cleanup
deleted knowledge base m3-outdoor-kb
deleted knowledge source m3-products-ks and index m3-products
deleted knowledge source m3-policies-ks and index m3-policies
deleted connection m3-outdoor-kb-mcp
```

The search service stays in place for the next topic. Delete it when the module is done:

```bash
az search service delete --name m3-procode-search --resource-group rg-pro-code-agents --yes
```
