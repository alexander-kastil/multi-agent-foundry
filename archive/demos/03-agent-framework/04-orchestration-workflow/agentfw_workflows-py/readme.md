# Agent Framework Workflows

## Overview

This collection demonstrates various workflow orchestration patterns using the Agent Framework for invoice processing scenarios. All samples share common functionality through `utils/invoice_utils.py`, which provides:

- **Invoice data model** (`InvoiceData`) with CSV loading
- **Configuration management** (`InvoiceConfig`) for tax rates, discounts, and thresholds
- **Calculation utilities** for subtotals, discounts (high-value and preferred client), and taxes
- **Rendering and I/O** for formatting invoices and saving to `output/invoices/`
- **Logging and archiving** with structured output to `output/logs/` and `output/archive/`

Each workflow demonstrates different orchestration patterns while processing the same invoice data, allowing for direct comparison of approaches.

## Workflow Samples

| Name                                   | Description                                                                                                                                                                                                                                                                                                                                         |
| -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `agentfw_sequential_workflow.py`       | Linear 5-step workflow: configuration → invoice selection → calculation → rendering → saving. **Contains `wait_for_user()` pauses between steps, requiring manual ENTER presses.**                                                                                                                                                                  |
| `agentfw_concurrent_workflow.py`       | Demonstrates parallel processing with fan-out/fan-in pattern. Dispatcher splits work into 3 concurrent tasks (totals calculator, client preparer, credit checker) that execute simultaneously, then merges results for final rendering. Shows independent task parallelism.                                                                         |
| `agentfw_branching_workflow.py`        | Conditional routing workflow with multiple decision points: checks for existing files to archive, applies discounts based on invoice value and client status (high-value/preferred/standard branches), demonstrates data-driven path selection.                                                                                                     |
| `agentfw_visualization_workflow.py`    | Interactive workflow pattern visualizer. Generates Mermaid diagrams (`.mmd` files) for sequential, parallel, and branching patterns. Outputs to `output/visualizations/` for documentation and presentations.                                                                                                                                       |
| `agentfw_interactive_checkpointing.py` | Human-in-the-loop workflow with automatic state persistence. Pauses for user confirmation on tax rates and discounts, saves checkpoints at each pause point, supports resume from interruption with full state restoration.                                                                                                                         |
| `agentfw_agents_in_workflow.py`        | AI agents integrated into workflow steps. Uses Azure AI agents for intelligent processing: analyzes invoices, makes business decisions, generates personalized communications, and creates executive summaries. Requires Azure AI Project configuration.                                                                                            |
| `agentfw_weather_devui.py`             | **Azure AI Agent with DevUI** - Interactive weather assistant using Azure AI Agents from Microsoft Foundry (agent service) with the Agent Framework DevUI. Features chat/function middleware for security filtering, function approval for sensitive operations (email), and demonstrates proper resource cleanup. Runs at `http://localhost:8090`. |

---

## Demo Guide: Azure Weather Agent with DevUI

### Overview

The `agentfw_weather_devui.py` demo showcases how to use **Azure AI Agents from Microsoft Foundry** (the agent service) with the **Agent Framework DevUI** for interactive debugging and testing. This demonstrates the migration from chat-based agents to cloud-hosted Azure AI Agents while maintaining the powerful DevUI interface.

### Key Features

- **Azure AI Agent Service**: Uses `AzureAIAgentClient` to connect to Azure AI Foundry-hosted agents
- **DevUI Integration**: Interactive web interface for testing and debugging at `http://localhost:8090`
- **Security Middleware**: Chat middleware that blocks sensitive information (passwords, secrets, API keys, tokens)
- **Location Filtering**: Function middleware that prevents weather requests for "Atlantis"
- **Function Approval**: Email sending requires explicit user approval (demonstrates human-in-the-loop)
- **Resource Cleanup**: Proper async credential cleanup on shutdown using `register_cleanup()`

### Prerequisites

1. **Azure AI Foundry Project**: You need an active Microsoft Foundry (formerly Azure AI Foundry) project with a model deployment
2. **Azure Authentication**: Logged in via Azure CLI (`az login`) or other credential methods
3. **Environment Variables**: Configure `.env` file with your project details

### Setup

1. **Configure Environment Variables**

   Create or update `.env` file in the project root:

   ```bash
   # Azure AI Foundry project configuration
   AZURE_AI_PROJECT_ENDPOINT=https://your-project.region.api.azureml.ms/api/projects/your-project
   AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4o-mini
   ```

2. **Authenticate with Azure**

   ```powershell
   az login
   ```

3. **Install Dependencies** (if not already done)

   ```powershell
   pip install -r requirements.txt
   ```

### Running the Demo

1. **Start the DevUI Server**

   ```powershell
   python agentfw_weather_devui.py
   ```

   The server will start on `http://localhost:8090` and automatically open in your browser.

2. **Interact with the Agent**

   Try these example prompts:

   - **Basic Weather**: `"What's the weather in Seattle?"`
   - **Forecast**: `"Give me a 5-day forecast for Paris"`
   - **Security Filter Test**: `"My password is secret123"` (will be blocked)
   - **Location Filter Test**: `"What's the weather in Atlantis?"` (will be blocked)
   - **Function Approval**: `"Send an email to john@example.com about the meeting"` (requires approval)

3. **Explore DevUI Features**

   - **Chat Interface**: Main conversation area with the agent
   - **Debug Panel**: View execution traces, function calls, and middleware actions
   - **Entity Details**: Inspect agent configuration, tools, and middleware
   - **Conversation History**: Review past interactions

### How It Works

#### Architecture

```
User → DevUI (Browser)
         ↓
    FastAPI Server (port 8090)
         ↓
    ChatAgent with Azure AI Agent Client
         ↓
    Microsoft Foundry Agent Service
         ↓
    Azure OpenAI Model (gpt-4o-mini)
```

#### Key Components

1. **Azure AI Agent Client**

   ```python
   chat_client=AzureAIAgentClient(
       project_endpoint=os.environ.get("AZURE_AI_PROJECT_ENDPOINT"),
       model_deployment_name=os.environ.get("AZURE_AI_MODEL_DEPLOYMENT_NAME"),
       async_credential=DefaultAzureCredential(),
       agent_name="AzureWeatherAgent",
   )
   ```

2. **Tools (Functions)**

   - `get_weather(location)`: Returns current weather
   - `get_forecast(location, days)`: Returns multi-day forecast
   - `send_email(recipient, subject, body)`: Sends email (requires approval)

3. **Middleware Stack**

   - **Security Filter**: Blocks messages containing sensitive terms
   - **Atlantis Filter**: Prevents weather queries for the mythical location

4. **Resource Cleanup**
   ```python
   register_cleanup(agent, cleanup_credential)
   ```
   Ensures proper credential disposal when DevUI shuts down.

### Testing Scenarios

#### Scenario 1: Normal Weather Request

```
User: "What's the weather like in Tokyo?"
Agent: Calls get_weather("Tokyo") → Returns weather information
```

#### Scenario 2: Security Middleware Trigger

```
User: "My API key is abc123, what's the weather?"
Agent: Blocked by security_filter_middleware
Response: "I cannot process requests containing sensitive information..."
```

#### Scenario 3: Location Filter Trigger

```
User: "What's the weather in Atlantis?"
Agent: Calls get_weather("Atlantis") → Blocked by atlantis_location_filter_middleware
Response: "Atlantis is a special place, we must never ask about the weather there!!"
```

#### Scenario 4: Function Approval Required

```
User: "Send an email to team@company.com about tomorrow's meeting"
Agent: Requests approval to call send_email()
DevUI: Shows approval dialog
User: Approves → Email "sent"
```

### Configuration Options

The agent can be customized through:

- **Model Selection**: Change `AZURE_AI_MODEL_DEPLOYMENT_NAME` to use different models
- **Port**: Modify `port=8090` in `serve()` call
- **Instructions**: Update the agent's system instructions
- **Tools**: Add/remove functions in the `tools=[]` parameter
- **Middleware**: Add custom middleware for additional filtering or processing

### Troubleshooting

**Error: "Azure AI project endpoint is required"**

- Ensure `.env` file exists with `AZURE_AI_PROJECT_ENDPOINT` set
- Verify `load_dotenv('.env')` is called before creating the agent

**Error: Authentication failed**

- Run `az login` to authenticate with Azure
- Verify your Azure account has access to the Foundry project

**DevUI doesn't open**

- Manually navigate to `http://localhost:8090`
- Check if port 8090 is already in use
- Review terminal output for errors

### Cleanup

Press `Ctrl+C` in the terminal to stop the server. The registered cleanup function will automatically close the Azure credential.
