<img src="./assets/search-results.png" alt="Example search result" width="520" />

# Policy Connector (TypeScript)

## Overview

This project packages a Microsoft Graph external connector that surfaces organization policy content to Microsoft 365 Copilot experiences. It is built with TypeScript on Azure Functions and automated through the Microsoft 365 Agents (formerly Teams Toolkit) workflow. At startup, a timer-triggered function guarantees that the target Graph connection, schema, and result template exist, then ingests curated Markdown documents from the `content/` directory into the connector so Copilot can search and ground answers on them.

## Key elements

- `src/functions/connections.ts` defines the timer-triggered `deployConnection` and `retractConnection` functions that orchestrate provisioning, schema management, and clean-up. In development the deploy function runs immediately to bootstrap a local environment.
- `src/connection.ts`, `src/schema.ts`, and `src/ingest.ts` encapsulate the Graph connector lifecycle: creating the connection, applying the schema and result template, ingesting items, and handling retries (including admin consent prompts) via Microsoft Graph SDK.
- `src/services/itemsService.ts` and `src/custom/` transform Markdown files—with YAML front matter—into strongly typed objects (`src/models/*.ts`) and eventually into `ExternalItem` payloads accepted by the Graph connector.
- `src/references/schema.json` and `template.json` contain the declarative schema and Adaptive Card result template that shape Copilot search responses.
- `content/` holds sample policy collections grouped by scenario (e.g., `it`, `financial-services`, `healthcare`). Each file includes metadata used during ingestion such as `id`, `title`, `abstract`, and `lastModified`.
- `scripts/admin-consent.js` and the npm script `npm run notify:admin-consent -- --clientId <AAD_APP_CLIENT_ID>` surface the tenant-wide admin consent URL when Graph permissions have not yet been granted.
- `env/.env.*`, `local.settings.json` (generated), and `src/config.ts` coordinate runtime configuration, combining Teams Toolkit environment variables with Azure Function app settings.
- `m365agents*.yml` capture the Microsoft 365 Agents Toolkit automation for provisioning and deployment, while `package.json` defines the build (`tsc`) pipeline and local tooling utilities such as Azurite and Prettier.

## Deployment & infrastructure

- `infra/azure.bicep` provisions the Azure footprint: an App Service plan, Function App (Node.js 18, v4 runtime), Storage Account with OAuth defaults, Application Insights + Log Analytics, and a Key Vault that stores the connector client secret. It also wires function app settings (connector metadata, Teams Toolkit environment marker, Azure credentials) and assigns Key Vault access via role assignment.
- `infra/azure.parameters.json` binds Bicep parameters to Teams Toolkit environment variables like `CONNECTOR_ID`, `CONNECTOR_BASE_URL`, and `TEAMSFX_ENV`, enabling environment-specific deployments.
- The Microsoft 365 Agents workflow (`m365agents.yml`) sequences provisioning: create/update the Entra ID app registration, deploy the Bicep template, and zip-deploy the function code. For local development `m365agents.local.yml` creates `local.settings.json` with storage and connector settings.
- Common deployment commands (run from the repo root after signing in with Teams Toolkit CLI):

```powershell
teamsapp provision --env dev
teamsapp deploy --env dev
```

Once deployed, the Function App’s timer trigger manages the connector lifecycle without manual intervention; use `teamsapp publish` if you also distribute Copilot assets beyond the API connector.

## Local development

### Prerequisites

- Node.js 18.x
- Azure Functions Core Tools v4
- Azurite (installed automatically via dev dependency) for storage emulation
- Access to a Microsoft 365 tenant with permissions to grant Graph `ExternalConnection.ReadWrite.OwnedBy` and `ExternalItem.ReadWrite.OwnedBy`

### Environment setup

1. Copy or edit `env/.env.local` to match your scenario and base URL.
2. Run the Teams Toolkit provision flow locally if you haven’t previously (`teamsapp provision --env local`) so `local.settings.json` is generated with the AAD secrets.
3. Grant tenant-wide admin consent using the generated AAD app ID:

	```powershell
	npm run notify:admin-consent -- --clientId <AAD_APP_CLIENT_ID>
	```

### Run the function host

```powershell
npm install
npm run storage
npm run watch
func start
```

- `npm run storage` launches the Azurite storage emulator expected by local settings.
- `npm run watch` keeps the TypeScript compiler running; alternatively use the VS Code “Run watch” task.
- When the Functions host starts (`func start`), the `deployConnection` timer runs immediately (because `runOnStartup` is enabled in development) and pushes the current Markdown content into the Graph connection.

## Content model

- Each Markdown file contains YAML front matter such as `id`, `title`, `abstract`, `author`, and `lastModified`. The ingestion pipeline strips Markdown formatting to create a plain-text body while preserving metadata for search facets.
- Update or add files under the appropriate scenario folder (`content/<scenario>/`) and restart the function host to re-ingest.
- The Adaptive Card template in `src/references/template.json` shapes Copilot search cards; adjust it to change the rendering of search results.

## Useful npm scripts

- `npm run prettier:write` keeps TypeScript and JSON files formatted.
- `npm run clean` removes compiled artifacts.
- `npm run build` performs a one-time TypeScript compilation (used by CI/CD before deployment).

## Troubleshooting

- **Authentication loops:** If the function logs prompt for admin consent, run the consent script above and restart the host. The retry logic in `connection.ts` will keep polling until Graph accepts the request.
- **Schema deployment latency:** Graph schema creation can take several minutes. The custom `LongRunningOperationMiddleware` in `src/longRunningOperationMiddleware.ts` handles retries automatically; allow the timer function to continue running until completion.

## Next steps

- Customize `content/` with your own policy or knowledge base documents.
- Extend `schema.json` and `getExternalItemFromItem.ts` if you need additional searchable fields.
- Automate ingestion by adjusting the timer CRON expressions in `src/functions/connections.ts` or by adding HTTP-triggered functions for on-demand refreshes.
