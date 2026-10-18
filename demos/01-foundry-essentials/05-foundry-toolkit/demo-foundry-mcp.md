# Operate Foundry with the MCP Server and Agent Inspector

Checking a deployment, creating a test agent and deleting it again are small tasks that cost minutes in a portal every time. The same tasks can be done from a terminal, from code or from a chat in your editor, because the Foundry MCP server exposes them as tools. In this demo you set up the `azd ai` tooling, drive the Foundry MCP server from Python to manage a real prompt agent, and inspect a local agent with Agent Inspector.

What you practice here: calling Foundry management operations through MCP tools with your own Entra identity, and debugging a local agent with Agent Inspector.

Duration: about 50 minutes.

## Prerequisites

- Azure Developer CLI 1.27.1 or later, Python 3.10 or later or the .NET 10 SDK, and Azure CLI signed in with `az login`.
- Contributor or higher on the Foundry project, because the MCP write tools need it. The Foundry MCP server is in preview.
- The model router deployments from the previous topic, so that step 3 has something to read. Any deployment name works if you pass it to the script.
- Optional for step 2: VS Code with GitHub Copilot and Foundry Toolkit installed.

## Run it

The MCP client scripts are in `foundry-mcp-py/` for Python and `foundry-mcp-cs/` for C#. The local agent used in steps 5 and 6 is in its own folder in each language, because it pulls the Agent Framework packages: `foundry-mcp-py/local-agent/` and `local-agent-cs/`.

```bash
cd demos/01-foundry-essentials/05-foundry-toolkit/foundry-mcp-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
FOUNDRY_ACCOUNT_RESOURCE_ID=/subscriptions/<subscription-id>/resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-foundry
```

The C# commands in steps 2 to 4 run from this same folder with `--project ../foundry-mcp-cs`, and .NET reads the `.env` of the folder you run from. Restore the C# project once:

```bash
dotnet restore ../foundry-mcp-cs
```

## Step 1: Install the Foundry azd extensions and point them at the project

The `microsoft.foundry` meta-package installs every `azd ai` command group in one step. `azd ai project set` stores the project endpoint so later commands need no flags.

```bash
azd ext install microsoft.foundry
azd ai project set "https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents"
azd ai project show
azd ai connection list
```

Expected output (versions and connections depend on your tools and project):

```text
Project endpoint set: https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
Project endpoint:  https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
Source:            global config (~/.azd/config.json)
Name                   Kind                 Auth Type               Target
----                   ----                 ---------               ------
m7-appinsights         AppInsights          ApiKey                  /subscriptions/cd091145-.../components/m7-appinsights
...
```

`azd ai connection list` read the project's connections with no endpoint flag, so the stored default works. `azd ext list` shows the installed bundle:

```text
ID                         NAME                                   STATUS          INSTALLED        LATEST
azure.ai.agents            Foundry agents (Beta)                  Up to date      1.0.0-beta.20    1.0.0-beta.
azure.ai.connections       Foundry Connections (Beta)             Up to date      1.0.0-beta.10    1.0.0-beta.
azure.ai.inspector         Foundry Agent Inspector (Beta)         Up to date      1.0.0-beta.8     1.0.0-beta.
azure.ai.projects          Foundry Projects (Beta)                Up to date      1.0.0-beta.14    1.0.0-beta.
microsoft.foundry          Microsoft Foundry (Beta)               Up to date      1.0.0-beta.2     1.0.0-beta.
```

## Step 2: Connect to the Foundry MCP server and list its tools

The server lives at `https://mcp.ai.azure.com` and expects an Entra token issued for that audience. `foundry_mcp.py` builds an MCP client that sends your `DefaultAzureCredential` token as a bearer header.

```python
MCP_URL = "https://mcp.ai.azure.com"


def connect():
    token = DefaultAzureCredential().get_token(f"{MCP_URL}/.default").token
    http = httpx2.AsyncClient(headers={"Authorization": f"Bearer {token}"}, timeout=120)
    return Client(streamable_http_client(MCP_URL, http_client=http))
```

```csharp
HttpClient http = new(new BearerTokenHandler(new DefaultAzureCredential(), $"{McpUrl}/.default"));

McpClient client = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(McpUrl),
        TransportMode = HttpTransportMode.StreamableHttp,
    },
    http));

IList<McpClientTool> tools = await client.ListToolsAsync();
```

`BearerTokenHandler` is a small `HttpClientHandler` in `FoundryMcp.cs` that asks `DefaultAzureCredential` for a token with the `https://mcp.ai.azure.com/.default` scope and sets it as the bearer header on every request. The `ModelContextProtocol` 2.2.0 package supplies `McpClient` and the transport.

```bash
.venv/Scripts/python list_tools.py
dotnet run --project ../foundry-mcp-cs -- tools
```

Expected output (the tool count changes during the preview):

```text
tools exposed: 80
agent        8
continuous   3
data         4
evaluation   20
evaluator    7
model        14
project      6
prompt       1
session      11
toolbox      6

agent_update
  projectEndpoint              string
  agentName                    string
  agentDefinition              object
  isCloneRequest               boolean
  cloneTargetAgentName         string
  modelName                    string
  creationOptions              object
model_deployment_get
  foundryAccountResourceId     string
  deploymentName               string
```

The C# command prints the same 80 tools and the same two schemas. Each tool carries an input schema, which is what a language model reads to build the call for you. The identifiers `projectEndpoint` and `foundryAccountResourceId` are the two values you have in your `.env`.

To use the same server from your editor, the project ships `.vscode/mcp.json`. Open `foundry-mcp-py/` in VS Code, run MCP: List Servers from the Command Palette, start `foundry-mcp-remote` and sign in. Then ask Copilot Agent Mode:

```text
Using the Foundry MCP server, show the model router deployments on my Foundry resource and which models m1-router-subset can route to.
```

Copilot asks for permission to run the read tool before it calls it. This editor path is not part of the captured output in this guide.

## Step 3: Read deployments through the MCP server

Read tools need only the Reader role. `read_tools.py` looks up the model router in the catalog and then reads one of your router deployments.

```python
result = await client.call_tool(
    "model_deployment_get",
    {
        "foundryAccountResourceId": os.environ["FOUNDRY_ACCOUNT_RESOURCE_ID"],
        "deploymentName": DEPLOYMENT,
    },
)
deployment = json.loads(result.content[0].text)[0]
```

```csharp
CallToolResult result = await client.CallToolAsync("model_deployment_get", new Dictionary<string, object?>
{
    ["foundryAccountResourceId"] = Environment.GetEnvironmentVariable("FOUNDRY_ACCOUNT_RESOURCE_ID"),
    ["deploymentName"] = deploymentName,
});
var deployment = JsonDocument.Parse(result.Content.OfType<TextContentBlock>().First().Text).RootElement[0];
```

```bash
.venv/Scripts/python read_tools.py
dotnet run --project ../foundry-mcp-cs -- read
```

Expected output:

```text
catalog   : model-router by Microsoft
asset     : azureml://registries/azureml-routers/models/model-router/versions/2025-11-18

deployment: m1-router-subset (ADM, Succeeded)
model     : model-router 2025-11-18
capacity  : GlobalStandard x 10
routes to : gpt-5-mini 2025-08-07
routes to : gpt-5.4 2026-03-05
```

The tool returned the subset you configured in the previous topic without a single REST call from your code. Pass another deployment name as the first argument to either command to read a different one. The C# command prints the same lines.

## Step 4: Create, invoke and delete a prompt agent with write tools

Write tools change live resources and need Contributor. `agent_lifecycle.py` creates a prompt agent with `agent_update`, sends it a message with `agent_invoke`, deletes it with `agent_delete` and checks that it is gone.

```python
DEFINITION = {
    "kind": "prompt",
    "model": os.environ["MODEL_DEPLOYMENT_NAME"],
    "instructions": "You explain Microsoft Foundry concepts in one short sentence and always call the product Microsoft Foundry.",
}

created = await client.call_tool(
    "agent_update",
    {"projectEndpoint": ENDPOINT, "agentName": AGENT, "agentDefinition": DEFINITION},
)
```

```csharp
var definition = new Dictionary<string, object?>
{
    ["kind"] = "prompt",
    ["model"] = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME"),
    ["instructions"] = "You explain Microsoft Foundry concepts in one short sentence and always call the product Microsoft Foundry.",
};

CallToolResult created = await client.CallToolAsync("agent_update", new Dictionary<string, object?>
{
    ["projectEndpoint"] = endpoint,
    ["agentName"] = "m1-mcp-agent",
    ["agentDefinition"] = definition,
});
```

```bash
.venv/Scripts/python agent_lifecycle.py
dotnet run --project ../foundry-mcp-cs -- lifecycle
```

Expected output (the answer varies):

```text
created   : m1-mcp-agent version 2 (prompt)
status    : completed
answer    : A Microsoft Foundry project is a workspace that organizes your AI app’s resources, settings, assets, and collaboration in one place.
deleted   : True
lookup    : not found
```

The agent existed only between the first and fourth line. Pass `agentDefinition` as an object and not as a JSON string, otherwise the tool call fails; in C# a `Dictionary<string, object?>` serializes to that object. The C# command prints the same five lines. The version number is 1 on a fresh project and counts up if an agent with that name already existed.

## Step 5: Run a local agent

Agent Inspector needs something to inspect. `local-agent/main.py` is a minimal Agent Framework agent with one tool, hosted on port 8088 with the Responses protocol. It installs into its own virtual environment because the Agent Framework hosting package is still a pre-release.

The .NET twin is `local-agent-cs/`. It builds the agent with `AsAIAgent`, registers it with `AddFoundryResponses` and maps `/responses` with `MapFoundryResponses`. The `Microsoft.Agents.AI.Foundry.Hosting` package is a preview and depends on `Azure.AI.Projects` 3.0.0-beta.3, so this one project pins that beta while every other C# project in the module stays on 2.0.1.

```python
agent = Agent(
    client=client,
    instructions="You are a weather assistant. Always use the get_weather tool and keep answers to one sentence.",
    tools=[get_weather],
    default_options={"store": False},
)
ResponsesHostServer(agent).run()
```

```csharp
AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You are a weather assistant. Always use the get_weather tool and keep answers to one sentence.",
        name: "weather-assistant",
        tools: [AIFunctionFactory.Create(GetWeather, "get_weather")]);

builder.Services.AddFoundryResponses(agent);
app.MapFoundryResponses();
```

Start the agent in one terminal. The Python server binds all network interfaces by default, so allow the firewall prompt only on a trusted network; the C# host binds `localhost` only.

```bash
cd local-agent
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp ../.env .env
.venv/Scripts/python main.py
```

For C#, start the host instead from `local-agent-cs/`:

```bash
cd demos/01-foundry-essentials/05-foundry-toolkit/local-agent-cs
cp ../foundry-mcp-py/.env .env
dotnet run
```

In a second terminal, send it a question with `ask.py` (Python) or with the `ask` command of the C# host, which posts to `/responses`:

```bash
cd demos/01-foundry-essentials/05-foundry-toolkit/foundry-mcp-py/local-agent
.venv/Scripts/python ask.py "What is the weather in Graz?"

cd ../../local-agent-cs
dotnet run -- ask "What is the weather in Graz?"
```

Expected output of `ask.py` and of the C# `ask` command:

```text
status : completed
items  : ['function_call', 'function_call_output', 'message']
answer : The weather in Graz is sunny with a high of 21°C.
```

The three items show that the agent called its tool before it answered. The first terminal logs `AgentServerHost started` and `Running on http://0.0.0.0:8088` for the Python agent, and `Now listening on: http://localhost:8088` for the C# host.

## Step 6: Inspect the agent with Agent Inspector

Leave the agent running, Python or C#, and start the inspector in a third terminal.

```bash
azd ai inspector launch
```

Expected output:

```text
Inspector:    http://127.0.0.1:8087
Target:       localhost:8088 (local)

Press Ctrl+C to stop the inspector.
```

The command opens the Agent Inspector UI in your browser, served from `http://127.0.0.1:8087` and connected to your agent on port 8088. Send the message from step 5 in the inspector playground. The response, the `get_weather` tool call with its arguments and the streamed events appear in the UI, and the streamed chunks are mirrored in the terminal that runs the inspector.

The browser part is described here and not captured. The check that was run for this guide is that the UI answers with HTTP 200 on port 8087 and names `localhost:8088` as its target.

In VS Code with Foundry Toolkit the same view opens from Developer Tools, Build, Agent Inspector, and pressing F5 on a generated hosted-agent project starts the agent and the inspector together.

## Verify

You have verified the demo when step 3 lists the two subset models, step 4 prints `deleted   : True` and `lookup    : not found`, and step 5 shows a `function_call` item before the final message.

## Cleanup

Stop the inspector and the local agent with Ctrl+C in their terminals. The C# `bin/` and `obj/` folders are git-ignored build output. The agent from step 4 is already deleted. Clear the stored azd default so other projects are not affected.

```bash
azd ai project unset
```
