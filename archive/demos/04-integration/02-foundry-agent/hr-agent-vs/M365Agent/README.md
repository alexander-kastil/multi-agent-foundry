# Contoso HR Custom Engine Agent

This project shows how to host a custom Microsoft Agents SDK engine that relays user conversations to an Azure AI Foundry agent while maintaining conversation state inside ASP.NET Core.

## Solution walk-through

| Step | What happens                                                                                                               | Where to look                    |
| ---- | -------------------------------------------------------------------------------------------------------------------------- | -------------------------------- |
| 1    | ASP.NET Core is bootstrapped, registering the Microsoft Agents pipeline, authentication, memory storage, and bot handlers. | `Program.cs`                     |
| 2    | `EchoBot` inherits from `AgentApplication` and wires activity handlers (`OnConversationUpdate`, `OnActivity`).             | `Bot/EchoBot.cs`                 |
| 3    | Conversation state helpers persist message counts and Azure AI thread identifiers between turns.                           | `ConversationStateExtensions.cs` |
| 4    | Configuration binds Azure AI Foundry project/agent identifiers so the bot can connect to the managed agent.                | `appsettings*.json`              |

## How the custom engine calls Azure AI Foundry

The `EchoBot` class encapsulates all communication with Azure AI Foundry:

1. **Connect to the project** – In the constructor, the bot reads `AIServices:ProjectEndpoint` and creates a `PersistentAgentsClient` with an `AzureCliCredential` to authenticate against Azure AI Foundry.
2. **Resolve the agent definition** – For each message, `OnMessageAsync` calls `Administration.GetAgentAsync` with the configured `AIServices:AgentID` to fetch the latest agent definition.
3. **Seed an Azure AI agent instance** – The agent definition and project client are passed into `AzureAIAgent`, giving the runtime access to the model, tools, and instructions configured in Azure AI Foundry.
4. **Manage conversation threads** – The bot stores the Foundry thread identifier in Microsoft Agents conversation state (`turnState.Conversation.ThreadId`). Returning visitors reuse the same thread so the Foundry agent retains context.
5. **Stream the response** – User messages are wrapped in Semantic Kernel `ChatMessageContent` and sent through `InvokeStreamingAsync`. As streaming chunks arrive, they are forwarded to the Teams conversation through `turnContext.StreamingResponse.QueueTextChunk`. The stream is closed with `EndStreamAsync`.

## Running the agent locally

1. Press **F5** (or Debug ➜ Start Debugging) in Visual Studio to launch the ASP.NET Core host.
2. In Microsoft 365 Agents Playground, send any message to the bot and watch the Foundry agent response stream back.
3. Update `appsettings.Development.json` with your own Azure AI Foundry `ProjectEndpoint` and `AgentID` values before testing.

The packaged Teams app can also run in Outlook and the Microsoft 365 app. Learn more at https://aka.ms/vs-ttk-debug-multi-profiles.

## Core packages used

| Package                                                                | Purpose in this solution                                                                                                              |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `Microsoft.Agents.Builder`                                             | Provides the `AgentApplication` base class, conversation state helpers, and dependency injection extensions used to register the bot. |
| `Microsoft.Agents.Hosting.AspNetCore`                                  | Integrates Microsoft Agents with ASP.NET Core request handling and authentication middleware.                                         |
| `Microsoft.Agents.Storage`                                             | Supplies the `MemoryStorage` implementation that stores conversation state during local development.                                  |
| `Azure.AI.Agents.Persistent`                                           | Exposes `PersistentAgentsClient` for Azure AI Foundry project and agent management APIs.                                              |
| `Microsoft.SemanticKernel` & `Microsoft.SemanticKernel.Agents.AzureAI` | Offer the `AzureAIAgent` and streaming abstractions that translate Microsoft Agents activities into Azure AI Foundry prompts.         |
| `Azure.Identity`                                                       | Provides the `AzureCliCredential` used to authenticate against Azure AI Foundry without embedding secrets.                            |

## 📁 M365Agent folder guide

The `M365Agent` directory contains the Teams app packaging assets and infrastructure-as-code that wrap the ASP.NET Core agent.

| Item                                                | Description                                                                                                                                                     |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `M365Agent.atkproj`                                 | Microsoft 365 Agents Toolkit project file describing how Visual Studio runs and packages the agent.                                                             |
| `launchSettings.json`                               | Debug profiles for running the toolkit project locally.                                                                                                         |
| `m365agents.yml` / `m365agents.local.yml`           | Deployment manifests that configure the bot endpoint, domains, and Teams app metadata for cloud or local debugging.                                             |
| `appPackage/manifest.json`                          | Teams app manifest referencing the bot, including icons (`color.png`, `outline.png`) and a prebuilt package in `appPackage/build/`.                             |
| `env/`                                              | Environment variable templates (`.env.*`) used by the toolkit for local or remote provisioning.                                                                 |
| `infra/azure.bicep` & `infra/azure.parameters.json` | Azure Resource Manager (Bicep) templates that provision supporting resources such as the bot registration; `infra/botRegistration/` contains generated outputs. |

For Microsoft 365 Agents Toolkit guidance, visit https://aka.ms/teams-toolkit-vs-docs. To report issues, use **Visual Studio ➜ Help ➜ Send Feedback ➜ Report a Problem** or open an issue at https://github.com/OfficeDev/TeamsFx/issues.
