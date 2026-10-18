# Foundry IQ Setup Checklist

This document outlines the required setup steps before running the `agents-foundry-iq.py` sample.

## Prerequisites

Before you can run the Foundry IQ sample, you need to set up several Azure resources and configurations.

### 1. Azure AI Search Knowledge Base (Required)

You need to create a knowledge base in Azure AI Search with at least one knowledge source.

**Steps:**

1. **Create/Configure Azure AI Search Service**

   - You already have the `procodeaisearch` connection configured
   - Ensure **Semantic Ranker** is enabled on your search service
   - Service must be in a [region that supports agentic retrieval](https://learn.microsoft.com/en-us/azure/search/search-region-support)

2. **Create a Knowledge Source**

   - Choose one of the supported knowledge sources:
     - Search Index (existing Azure AI Search index with semantic configuration)
     - Blob Storage (Azure Storage with documents)
     - SharePoint (remote SharePoint site)
     - OneLake (Microsoft Fabric)
   - Follow the guide: [Create Knowledge Sources](https://learn.microsoft.com/en-us/azure/search/agentic-knowledge-source-overview)

3. **Create a Knowledge Base**
   - Knowledge base references one or more knowledge sources
   - Requires an LLM deployment (gpt-4o, gpt-4.1-mini, etc.) for query planning
   - Follow the guide: [Create Knowledge Base](https://learn.microsoft.com/en-us/azure/search/agentic-retrieval-how-to-create-knowledge-base)
   - Uses API version: `2025-11-01-preview`

**Example using Python SDK:**

```python
from azure.search.documents import SearchClient
from azure.core.credentials import AzureKeyCredential

# Create knowledge source (search index example)
# Then create knowledge base that references it
```

### 2. Project Connection for MCP (Required)

The sample requires a project connection that uses the project's managed identity to target the MCP endpoint of the knowledge base.

**Connection Details:**

- **Name**: `foundry-iq-connection` (or customize in `.env`)
- **Type**: `RemoteTool`
- **Category**: `RemoteTool`
- **Auth Type**: `ProjectManagedIdentity`
- **Target**: `{search_endpoint}/knowledgebases/{knowledge_base_name}/mcp?api-version=2025-11-01-preview`
- **Audience**: `https://search.azure.com/`

**Option A: Create via Python (Recommended)**

Add this code to the sample or create a setup script:

```python
import requests
from azure.identity import DefaultAzureCredential, get_bearer_token_provider

credential = DefaultAzureCredential()
project_resource_id = "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.MachineLearningServices/workspaces/{account}/projects/{project}"
project_connection_name = "foundry-iq-connection"
mcp_endpoint = "{search_endpoint}/knowledgebases/{kb_name}/mcp?api-version=2025-11-01-preview"

bearer_token_provider = get_bearer_token_provider(credential, "https://management.azure.com/.default")
headers = {"Authorization": f"Bearer {bearer_token_provider()}"}

response = requests.put(
    f"https://management.azure.com{project_resource_id}/connections/{project_connection_name}?api-version=2025-10-01-preview",
    headers=headers,
    json={
        "name": project_connection_name,
        "type": "Microsoft.MachineLearningServices/workspaces/connections",
        "properties": {
            "authType": "ProjectManagedIdentity",
            "category": "RemoteTool",
            "target": mcp_endpoint,
            "isSharedToAll": True,
            "audience": "https://search.azure.com/",
            "metadata": {"ApiType": "Azure"}
        }
    }
)
response.raise_for_status()
```

**Option B: Create via Azure CLI**

```bash
az account get-access-token --scope https://management.azure.com/.default --query accessToken -o tsv

# Then make PUT request to Azure Management API
```

### 3. Environment Variables (Required)

Update `.env` file with the correct values:

```env
# Existing variables
PROJECT_ENDPOINT="https://pro-code-agents-resource.services.ai.azure.com/api/projects/pro-code-agents"
MODEL_DEPLOYMENT="gpt-4o"
AZURE_AI_SEARCH_CONNECTION="procodeaisearch"

# New Foundry IQ variables
KNOWLEDGE_BASE_NAME="my-knowledge-base"           # Name of your knowledge base in Azure AI Search
FOUNDRY_IQ_CONNECTION="foundry-iq-connection"     # Name of the project connection for MCP
```

### 4. Azure Roles and Permissions (Required)

**Microsoft Foundry Project:**

- **Azure AI User** role - to access model deployments and create agents (auto-assigned to Owners)
- **Azure AI Project Manager** role - to create project connections for MCP authentication
- Enable **system-assigned managed identity** on your project

**Azure AI Search Service:**

- Assign **Search Index Data Reader** role to your project's managed identity
- (Optional) **Search Index Data Contributor** role if the agent needs to write documents
- (Optional) For SharePoint knowledge sources, include ACL fields and pass user tokens

**Steps to assign roles:**

```bash
# Get your project's managed identity principal ID
PROJECT_IDENTITY=$(az ml workspace show --name pro-code-agents --resource-group rg-pro-code-agents --query identity.principalId -o tsv)

# Assign Search Index Data Reader role
az role assignment create \
  --assignee $PROJECT_IDENTITY \
  --role "Search Index Data Reader" \
  --scope /subscriptions/{subscription-id}/resourceGroups/{rg}/providers/Microsoft.Search/searchServices/{search-service}
```

### 5. Python SDK Requirements (Required)

Install the latest preview SDK that supports Foundry IQ:

```bash
pip install azure-ai-projects azure-ai-foundry azure-identity
```

Or using `uv`:

```bash
uv pip install azure-ai-projects azure-ai-foundry azure-identity
```

## Validation Checklist

Before running `agents-foundry-iq.py`, verify:

- [ ] Azure AI Search service has semantic ranker enabled
- [ ] Knowledge source created in Azure AI Search
- [ ] Knowledge base created with at least one knowledge source
- [ ] Project connection `foundry-iq-connection` exists (or code creates it)
- [ ] Project has system-assigned managed identity enabled
- [ ] Managed identity has Search Index Data Reader role on search service
- [ ] Environment variables set correctly in `.env`
- [ ] Python SDK packages installed (`azure-ai-projects`, `azure-ai-foundry`)
- [ ] Azure credentials configured (DefaultAzureCredential works)

## Running the Sample

Once setup is complete:

```bash
cd demos/02-agent-service/02-knowledge-tools/agent-knowledge-tools-py
python agents-foundry-iq.py
```

## Troubleshooting

**"Connection not found" error:**

- Verify the connection name in `.env` matches the actual connection
- Check that the connection was created successfully
- Try creating the connection programmatically

**"Knowledge base not found" error:**

- Verify the knowledge base name is correct
- Check that the knowledge base exists in Azure AI Search
- Ensure the MCP endpoint URL is properly formatted

**"Permission denied" errors:**

- Verify managed identity is enabled on the project
- Check role assignments on Azure AI Search service
- Ensure you have the required roles on the Foundry project

**"No results returned" error:**

- Verify knowledge sources contain data
- Check that the semantic configuration is correct on the search index
- Try a different query that matches your knowledge base content

## Next Steps

After successful setup, you can:

1. Customize the agent instructions for your use case
2. Add multiple knowledge sources to the knowledge base
3. Implement SharePoint integration with the `x-ms-query-source-authorization` header
4. Add more complex queries and conversation flows
5. Integrate with other agent tools (code interpreter, function calling, etc.)

## References

- [Foundry IQ Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/knowledge-retrieval?view=foundry)
- [Create Knowledge Base](https://learn.microsoft.com/en-us/azure/search/agentic-retrieval-how-to-create-knowledge-base)
- [Knowledge Sources Overview](https://learn.microsoft.com/en-us/azure/search/agentic-knowledge-source-overview)
- [RBAC in Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/rbac-azure-ai-foundry)
