# Introduction to Azure AI Studio

- Core Features and Capabilities of Azure AI Studio
- Azure AI Hubs & Projects
- Deploying and Managing AI Applications

## Deploying and Managing AI Applications

The "Azure AI Studio Starter" template is recommended for users looking to get started with Azure AI Studio.

Resources Included:

- AI Hub with dependent resources
- AI project
- AI Services
- An online endpoint

### Prerequisites

Requires [Azure Developer CLI](https://learn.microsoft.com/de-de/azure/developer/azure-developer-cli/overview?tabs=linux) installation:

```bash
winget add Microsoft.Azd
```

### Initialize & Deployment

[Azure AI Templates with Azure Developer CLI](https://learn.microsoft.com/en-us/collections/5pq0uompdgje8d?sharingId=ADFFF9D4AD9A0F29&WT_mc.id=aip-114567-cassieb)

Initialize the Azure AI Studio Starter template:

```bash
azd init --template https://github.com/Azure-Samples/azd-aistudio-starter
```

> Note: Additional templates would be `azd init --template Azure-Samples/azure-search-openai-demo` or `azd init --template Azure-Samples/azure-enterprise-rag`

```bash
azd up
```

> Note: The `Azure AI Studio Starter template` has been customized to deploy the `gpt-4o-mini` model by default. Also it does not deploy the `Machine Learning Workspace`.

### Azure AI Studio Starter Template

#### Quickstart

To learn how to get started with any template, follow the steps in [this quickstart](https://learn.microsoft.com/azure/developer/azure-developer-cli/get-started?tabs=localinstall&pivots=programming-language-nodejs) with this template(`Azure-Samples/azd-aistudio-starter`)

This quickstart will show you how to authenticate on Azure, initialize using a template, provision infrastructure and deploy code on Azure via the following commands:

```bash
# Log in to azd. Only required once per-install.
azd auth login

# First-time project setup. Initialize a project in the current directory, using this template.
azd init --template Azure-Samples/azd-aistudio-starter

# Provision and deploy to Azure
azd up
```

#### Provisioned Azure Resources

This template creates everything you need to get started with Azure AI Studio:

- [AI Hub Resource](https://learn.microsoft.com/azure/ai-studio/concepts/ai-resources)
- [AI Project](https://learn.microsoft.com/azure/ai-studio/how-to/create-projects)
- [OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Online Endpoint](https://learn.microsoft.com/azure/machine-learning/concept-endpoints-online?view=azureml-api-2)
- [AI Search Service](https://learn.microsoft.com/azure/search/) _(Optional, enabled by default)_

The provisioning will also deploy any models specified within the `./infra/ai.yaml`.

For a list of supported models see [Azure OpenAI Service Models documentation](https://learn.microsoft.com/azure/ai-services/openai/concepts/models)

The template also includes dependent resources required by all AI Hub resources:

- [Storage Account](https://learn.microsoft.com/azure/storage/blobs/)
- [Key Vault](https://learn.microsoft.com/azure/key-vault/general/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview) _(Optional, enabled by default)_
- [Container Registry](https://learn.microsoft.com/azure/container-registry/) _(Optional, enabled by default)_

#### Optional Configuration

- To disable AI Search, run `azd config set USE_SEARCH_SERVICE false`
- To disable Application Insights, run `azd config set USE_APPLICATION_INSIGHTS false`
- To disable Container Registry, run `azd config set USE_CONTAINER_REGISTRY false`

By default this template will use a default naming convention to prevent naming collisions within Azure.
To override default naming conventions the following can be set.

- `AZUREAI_HUB_NAME` - The name of the AI Studio Hub resource
- `AZUREAI_PROJECT_NAME` - The name of the AI Studio Project
- `AZUREAI_ENDPOINT_NAME` - The name of the AI Studio online endpoint used for deployments
- `AZURE_OPENAI_NAME` - The name of the Azure OpenAI service
- `AZURE_SEARCH_SERVICE_NAME` - The name of the Azure Search service
- `AZURE_STORAGE_ACCOUNT_NAME` - The name of the Storage Account
- `AZURE_KEYVAULT_NAME` - The name of the Key Vault
- `AZURE_CONTAINER_REGISTRY_NAME` - The name of the container registry
- `AZURE_APPLICATION_INSIGHTS_NAME` - The name of the Application Insights instance
- `AZURE_LOG_ANALYTICS_WORKSPACE_NAME` - The name of the Log Analytics workspace used by Application Insights

Run `azd config set <key> <value>` after initializing the template to override the resource names
