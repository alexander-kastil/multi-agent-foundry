# Weather Forecast Agent (Microsoft Agents SDK Sample)

## Overview

This sample demonstrates a **custom engine agent** built with the Microsoft Agents SDK and Semantic Kernel. The agent delivers weather forecasts, can ask clarifying questions, and formats results as Adaptive Cards before replying through a bot endpoint.

## How the Agent Works

1. **Host startup (`Program.cs`)** – Configures ASP.NET Core services, registers a `Kernel`, wires Azure OpenAI chat completion, and exposes `/api/messages` so the `IAgentHttpAdapter` can hand off incoming activities to the bot runtime.
2. **Bot orchestration (`Bot/WeatherAgentBot.cs`)** – Derives from `AgentApplication`, intercepts message activities, starts a streaming response, and instantiates a fresh `WeatherForecastAgent` per turn while preserving conversation history in turn state.
3. **Agent definition (`Bot/Agents/WeatherForecastAgent.cs`)** – Builds a `ChatCompletionAgent` with guard-rail instructions, requests JSON-only answers, and exposes Semantic Kernel plugins (date/time, forecast lookup, adaptive card generation) to function-calling.
4. **LLM invocation** – The agent streams model output back into chat history, concatenates it, and enforces a simple schema containing `contentType` and `content`. Any malformed payload triggers a self-correcting retry prompt.
5. **Channel response** – Depending on the parsed `contentType`, the bot either streams text or returns an Adaptive Card attachment as the final message before closing the stream.

## Key Files

- `Program.cs` – ASP.NET Core host wiring, Semantic Kernel setup, Azure OpenAI registration, and bot endpoint configuration.
- `Bot/WeatherAgentBot.cs` – Microsoft Agents bot pipeline that manages turn state, streaming responses, and invokes the custom engine agent.
- `Bot/Agents/WeatherForecastAgent.cs` – Defines the `ChatCompletionAgent`, supplies instructions, registers plugins, and normalizes the LLM response.
- `Bot/Agents/WeatherForecastAgentResponse.cs` – Strongly typed result object that maps the enforced JSON schema to C# types.
- `Bot/Plugins/DateTimePlugin.cs` – Semantic Kernel plugin exposing date/time helpers to the agent.
- `Bot/Plugins/WeatherForecastPlugin.cs` – Mock weather lookup plugin that queues informative streaming updates during execution.
- `Bot/Plugins/AdaptiveCardPlugin.cs` – Uses the kernel’s chat completion service to turn forecast data into Adaptive Card JSON.
- `appsettings*.json` – Hold configuration values such as Azure OpenAI endpoint, deployment, and API key references.

## NuGet Packages

- `Microsoft.SemanticKernel.Agents.Core` / `Microsoft.SemanticKernel.Agents.AzureAI` – Provide the agent abstractions and Azure AI integration used to define and run the custom engine agent.
- `Microsoft.SemanticKernel.Connectors.AzureOpenAI` & `Microsoft.SemanticKernel.Connectors.OpenAI` – Enable the Semantic Kernel `Kernel` to talk to Azure OpenAI (and fallback OpenAI) chat completion services.
- `Microsoft.Agents.Hosting.AspNetCore` & `Microsoft.Agents.Authentication.Msal` – Host the bot as an ASP.NET Core app and handle token validation for incoming requests.
- `AdaptiveCards` – Supplies Adaptive Card schema types used when returning structured card responses.
- `Azure.Identity` – Supports Azure AD-based credential flows if you swap from API keys to managed identities.

## Try It

1. Populate `appsettings.Development.json` with your Azure OpenAI endpoint, deployment name, and API key.
2. Run the project locally (for example, `dotnet run`) and send activities to `POST /api/messages` through the Bot Framework Emulator or an integrated channel.

## Next Steps

- Replace the mock service in `WeatherForecastPlugin` with a real weather API call.
- Expand the schema in `WeatherForecastAgentResponse` to include additional forecast details (humidity, precipitation chance, etc.).

## .M365Agent package & IaC

The `.M365Agent` folder contains the Teams app scaffolding plus infrastructure-as-code assets needed to ship this agent into Microsoft 365:

- `appPackage/manifest.json` – Teams app manifest that links the custom engine agent (`copilotAgents.customEngineAgents`) to the bot ID produced during deployment. The folder also holds the icon pair (`color.png`, `outline.png`).
- `env/` – Environment-specific `.env` templates that Microsoft 365 Agents Toolkit populates with secrets such as `SECRET_AZURE_OPENAI_API_KEY` and resource IDs after provisioning.
- `infra/azure.bicep` – Main Bicep template deploying the web app, user-assigned managed identity, app settings, and wiring secrets for Azure OpenAI. It also invokes the nested `botRegistration/azurebot.bicep` module.
- `infra/azure.parameters.json` – Parameter file that stitches together environment variables with the Bicep template (resource suffix, OpenAI credentials, SKU, bot display name).
- `infra/botRegistration/azurebot.bicep` – Registers the hosted endpoint as a Bot Framework resource and enables the Microsoft Teams channel.
- `m365agents.yml` / `m365agents.local.yml` – Workflow definitions used by Microsoft 365 Agents Toolkit to provision and deploy resources across environments.
- `M365Agent.atkproj` – Toolkit project file that Visual Studio uses to coordinate packaging, debugging, and deployment tasks for the Teams app.
