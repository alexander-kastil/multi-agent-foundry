# Azure AI Foundry Deployment

This folder contains infrastructure as code (IaC) and deployment scripts for Azure AI Foundry resources.

## Contents

- `infra/` - Bicep infrastructure templates
- `get-keys.sh` - Script to retrieve Azure resource keys and generate configuration files
- `azure.yaml` - Azure Developer CLI configuration

## Prerequisites

- Azure CLI installed and authenticated (`az login`)
- Deployed Azure resources (using `azd up` or manual deployment)

## Usage

### Retrieving Configuration Keys

The `get-keys.sh` script retrieves keys and connection strings from your deployed Azure resources and generates configuration files for use in demos and labs.

#### Running the Script

```bash
# Interactive mode (prompts for resource group)
./get-keys.sh

# With resource group parameter
./get-keys.sh --resource-group your-resource-group-name
```

#### Generated Files

The script creates two configuration files in the **same directory** as the script:

1. **`.env`** - Environment variables file for Python projects
   - Contains all Azure resource connection strings and keys
   - Format: `KEY="value"`
   - Automatically ignored by git (secrets should not be committed)

2. **`appsettings.json`** - Configuration file for C# .NET projects
   - Contains structured configuration under `AzureConfig` section
   - Format: JSON with nested configuration
   - **Note**: This file contains secrets and should not be committed to git

#### Configuration Variables

The generated files include:

**Storage Account:**
- `STORAGE_ACCOUNT_NAME` / `AZURE_STORAGE_ACCOUNT_NAME`
- `STORAGE_ACCOUNT_KEY` / `AZURE_STORAGE_ACCOUNT_KEY`
- `STORAGE_CONNECTION_STRING` / `AZURE_STORAGE_CONNECTION_STRING`
- `STORAGE_CONTAINER_NAME`

**Azure AI Search:**
- `SEARCH_SERVICE_NAME`
- `SEARCH_SERVICE_ENDPOINT`
- `SEARCH_ADMIN_KEY`

**Azure AI Foundry:**
- `AI_FOUNDRY_HUB_NAME`
- `AI_FOUNDRY_PROJECT_NAME`
- `AI_FOUNDRY_ENDPOINT`
- `AI_FOUNDRY_KEY`
- `AI_FOUNDRY_HUB_ENDPOINT`
- `AI_FOUNDRY_PROJECT_ENDPOINT`
- `PROJECT_ENDPOINT`

**Azure OpenAI (compatibility):**
- `AZURE_OPENAI_SERVICE_NAME`
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_KEY`
- `AZURE_OPENAI_DEPLOYMENT_NAME`
- `AZURE_AI_MODELS_ENDPOINT`
- `AZURE_AI_MODELS_KEY`

**Other Resources:**
- `LOG_ANALYTICS_WORKSPACE_NAME`
- `EMBEDDINGS_MODEL`
- `MODEL_DEPLOYMENT_NAME`
- `COSMOS_ENDPOINT`, `COSMOS_KEY`, `COSMOS_CONNECTION_STRING` (if Cosmos DB is deployed)
- `ACR_NAME`, `ACR_USERNAME`, `ACR_PASSWORD` (if Container Registry is deployed)

### Infrastructure Deployment

The `infra/` folder contains Bicep templates that deploy:

- Azure Storage Account
- Azure AI Search Service
- Azure AI Foundry Hub
- Azure AI Foundry Project
- Log Analytics Workspace
- Application Insights
- Model Deployments (gpt-4o-mini, text-embedding-ada-002)

Deploy using Azure Developer CLI:

```bash
azd up
```

Or using Azure CLI directly:

```bash
az deployment group create \
  --resource-group your-rg \
  --template-file infra/main.bicep
```

## Security Notes

⚠️ **Important**: The generated `.env` and `appsettings.json` files contain sensitive credentials:

- Never commit these files to version control
- Store them securely on your local machine
- The `.env` file is automatically ignored by git
- The `appsettings.json` contains real secrets - do not commit it to the repository

## Troubleshooting

If the script reports missing services:
1. Verify your deployment completed successfully
2. Check that you're using the correct resource group name
3. Ensure you have appropriate permissions to read resource keys
4. Re-run `azd up` if needed to deploy missing resources
