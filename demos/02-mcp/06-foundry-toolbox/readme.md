# Foundry Toolbox: Managed MCP Endpoints

Every agent that connects to its own tools repeats the same work. Each one carries tool definitions, credentials and approval rules, and a change to one tool means touching every agent that uses it. A toolbox in Microsoft Foundry moves that work to one place: you define a curated set of tools once, and Foundry exposes them through a single MCP-compatible endpoint that any MCP client can consume.

This topic creates a toolbox with the Python SDK, reads its tools through the managed endpoint, consumes it from Agent Framework in Python and .NET, and releases a new version without touching the agent.

## What a Toolbox Is

A toolbox is a managed resource in a Foundry project. It holds versions, each version is an immutable list of tools, and one version is the default. Foundry covers four jobs with it.

| Pillar | What the toolbox does |
| --- | --- |
| Build | Bundle tools and skills, publish them once, configure authentication centrally |
| Discover | Tool search (preview) keeps hundreds of tools out of the model context |
| Consume | One MCP endpoint, usable from Foundry agents, Agent Framework, LangGraph or your own code |
| Govern | Authentication, guardrails, approvals and version management at the toolbox level |

A toolbox can contain MCP servers, web search, Azure AI Search, code interpreter, file search, OpenAPI tools, agent-to-agent tools and more. Function calling is not supported in a toolbox because it executes on the client.

## The Two Endpoints

Each toolbox has a consumer endpoint and one endpoint per version.

| Role | Endpoint | Serves |
| --- | --- | --- |
| Consumer | `{project_endpoint}/toolboxes/{name}/mcp?api-version=v1` | Always the default version |
| Developer | `{project_endpoint}/toolboxes/{name}/versions/{version}/mcp?api-version=v1` | One specific version, for testing before promotion |

Connect agents to the consumer endpoint. Promoting a new default version changes what that endpoint serves, so the agent code and its deployment stay as they are. Clients authenticate to either endpoint with a Microsoft Entra token for the scope `https://ai.azure.com/.default`, and the identity needs the Foundry User role on the project.

## Create a Toolbox

The stable client in `azure-ai-projects` exposes `project.toolboxes`. `create_version` creates the toolbox when it does not exist, and the first version becomes the default.

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import MCPToolboxTool
from azure.identity import DefaultAzureCredential

project = AIProjectClient(endpoint=PROJECT_ENDPOINT, credential=DefaultAzureCredential())

version = project.toolboxes.create_version(
    name="m2-docs-and-snippets",
    description="Microsoft Learn search plus the snippets server hosted on Azure Functions",
    tools=[
        MCPToolboxTool(
            server_label="mslearn",
            server_url="https://learn.microsoft.com/api/mcp",
            description="Search and read Microsoft Learn documentation",
            require_approval="never",
        ),
        MCPToolboxTool(
            server_label="snippets",
            server_url=SNIPPETS_MCP_URL,
            project_connection_id="m2-func-mcp-key",
            description="Save and read named code snippets",
            require_approval="never",
        ),
    ],
)
```

The second tool uses the project connection from the previous topic, so the key never reaches an agent. Add a `description` to every tool, because the model uses it to choose. With `azd` version 1.27.1 or later and the `microsoft.foundry` extension, the same toolbox is a YAML file and one command.

```bash
azd ext install microsoft.foundry
azd ai toolbox create m2-docs-and-snippets --from-file ./my-toolbox.yaml
```

The azd commands come from Microsoft Learn and were not run in this course. A toolbox may contain at most one unnamed tool of each built-in type, and every MCP tool needs a unique `server_label`.

## Tool Names in a Toolbox

The toolbox prefixes each MCP tool with its server label. An MCP client sees the name `snippets___get_snippet`, with three underscores between the label and the tool, because several servers can expose the same tool name.

```text
mslearn___microsoft_code_sample_search
mslearn___microsoft_docs_fetch
mslearn___microsoft_docs_search
snippets___get_snippet
snippets___hello_mcp
snippets___save_snippet
```

The Learn article describes the prefix with a dot (`myserver.get_info`). The endpoint in this course returned the triple-underscore form and rejected the dotted name with `No tool config matches tool name`. Read the names from `tools/list` instead of building them.

## Read the Tools with Any MCP Client

The toolbox endpoint is plain MCP over Streamable HTTP with a bearer token, so the Python MCP client reads it directly.

```python
token = DefaultAzureCredential().get_token("https://ai.azure.com/.default").token
async with streamablehttp_client(URL, headers={"Authorization": f"Bearer {token}"}) as (read, write, _):
    async with ClientSession(read, write) as session:
        await session.initialize()
        tools = await session.list_tools()
```

Two server limits matter when you write your own client. The toolbox does not implement `ping` or `prompts/list`, so skip them, and use streaming for `tools/call`.

## Consume from Agent Framework

In Python, `FoundryToolbox` wraps `MCPStreamableHTTPTool`. It adds the bearer token on every request, sends the preview feature header that the toolbox endpoint needs, and forwards the caller context when the agent runs as a hosted agent. The class lives in the `agent-framework-foundry-hosting` package, which is a beta release.

```python
from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient, FoundryToolbox

toolbox = FoundryToolbox(credential, url=TOOLBOX_URL)
async with Agent(client=client, instructions="Answer from Microsoft Learn and keep the snippets collection current.", tools=[toolbox]) as agent:
    result = await agent.run("Look up the azd command that sets the project endpoint, then save it as a snippet.")
```

In .NET, `AddFoundryToolboxes` registers a toolbox with a hosted agent. The integration resolves the managed endpoint, authenticates the requests and includes the toolbox in the readiness probe. The package `Microsoft.Agents.AI.Foundry.Hosting` is in preview.

```csharp
var credential = new DefaultAzureCredential();
AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), credential)
    .AsAIAgent(model: deploymentName, instructions: "Answer from Microsoft Learn and keep the snippets collection current.", name: "m2-toolbox-host");

builder.Services.AddFoundryResponses(agent);
builder.Services.AddFoundryToolboxes(credential, toolboxName);

app.MapFoundryResponses();
```

The host logs the number of tools it discovered when it reads the toolbox at startup, and it serves the Responses protocol on `/responses`.

## Versions and Promotion

Each call to `create_version` produces a new immutable version. It does not change the default, so you can test the new version on its developer endpoint first.

```python
version = project.toolboxes.create_version(name=NAME, description="Snippets only", tools=[...])
toolbox = project.toolboxes.update(NAME, default_version="2")
```

After `update`, the consumer endpoint serves version 2. Agents that were already connected to the consumer endpoint see the change without a redeployment. To roll back, call `update` with the earlier version.

## Approvals and Guardrails

The toolbox returns a `_meta.tool_configuration` object for every tool in `tools/list`. When a tool has `require_approval` set to `always`, the agent runtime must ask the user before it calls the tool. The endpoint does not block the call, so enforcement is the job of the runtime.

A toolbox version can also reference a guardrail policy by name with `policies.rai_config.rai_policy_name`, which filters tool inputs and outputs at the toolbox level.

## Preview Add-Ons

Two capabilities are in preview, and the demo does not use them.

| Capability | What it does | How it appears |
| --- | --- | --- |
| Tool search | Hides the tool list and exposes two meta-tools, `tool_search` and `call_tool`; you can pin tools and add context | `ToolSearchToolboxTool()` in the version |
| Skills | Reusable multi-step workflows, versioned and immutable, attached to a version | `ToolboxSkillReference(name="greeting")` in `skills`, served as MCP resources `skill://{name}` |

Use tool search when a toolbox grows past a few dozen tools, because every definition costs input tokens and makes the right choice harder. Toolbox integration in prompt agents is also preview and is available in the Visual Studio Code extension.

## Hands-On Demo

- [Publish Tools Once with a Foundry Toolbox](./demo-foundry-toolbox.md)

## Key Topics

- [What is Toolbox in Foundry?](https://learn.microsoft.com/azure/foundry/agents/concepts/toolbox-overview)
- [Create and manage a toolbox in Foundry](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/toolbox)
- [Use a toolbox with a hosted agent](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/use-toolbox-hosted-agent)
- [Use tool search (preview)](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/tool-search)
- [Use skills in Foundry (preview)](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/skills)
- [Toolbox authentication](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/tool-authentication)
- [Toolbox quickstart](https://learn.microsoft.com/azure/foundry/agents/quickstarts/quickstart-toolbox-agent)
