# Foundry Toolkit & Foundry MCP Server

Building agents is more than writing Python. You deploy models, inspect what an agent did, check the state of a project and clean up after tests, and the portal is a slow place to do that repeatedly. Foundry gives you three faster surfaces: the Foundry Toolkit in VS Code, the `azd ai` command line and the Foundry MCP server.

## Foundry Toolkit for VS Code

Foundry Toolkit for Visual Studio Code is the new name of AI Toolkit. It also absorbed the capabilities of the separate Microsoft Foundry extension, so local resources and Foundry resources are managed in one place. It needs the .NET runtime and installs from the Visual Studio Marketplace at `aka.ms/foundrytk`.

| Section | What it holds |
| --- | --- |
| My Resources | Models, agents, tools, knowledge sources and evaluations in your Foundry project, plus local resources |
| Developer Tools | Model Catalog, Model Playground, Agent Builder, Tool Catalog, Agent Inspector, deployment, evaluation and tracing |
| Help And Feedback | Documentation, release notes, issue reporting |

The tools you use most in this class are Agent Builder (create and version prompt agents), the Tool Catalog (find Foundry tools, MCP servers and toolboxes) and Agent Inspector (debug a local agent). Some Toolkit experiences are in preview, so check the release notes for the version you installed.

## Foundry DevPack and the azd extensions

Foundry DevPack installs the command-line tooling and the editor tooling together. On Windows it is one command, and it adds the Toolkit only when VS Code is present.

```powershell
winget install Microsoft.FoundryDevPack
```

If you want only the command line, install the Azure Developer CLI and then the Foundry extension bundle. `microsoft.foundry` is a meta-package that installs every extension below. Use the `azd` version the current docs name, which is 1.27.1 or later for the latest extension builds.

```bash
azd ext install microsoft.foundry
azd ext list
```

| Extension | Command group | What it does |
| --- | --- | --- |
| `azure.ai.agents` | `azd ai agent` | Initialize, run, invoke, deploy and monitor agents |
| `azure.ai.inspector` | `azd ai inspector` | Browser-based inspector for locally running agents |
| `azure.ai.projects` | `azd ai project` | Set, show and clear the default project endpoint |
| `azure.ai.connections` | `azd ai connection` | Manage project connections |
| `azure.ai.toolboxes` | `azd ai toolbox` | Manage versioned toolboxes |
| `azure.ai.routines` | `azd ai routine` | Manage timers, schedules and event triggers |
| `azure.ai.skills` | `azd ai skill` | Manage reusable agent skills |

The commands resolve the project from a persisted default, from `FOUNDRY_PROJECT_ENDPOINT` in the active azd environment or from the same variable in your shell.

```bash
azd ai project set "https://<resource>.services.ai.azure.com/api/projects/<project>"
azd ai project show
azd ai connection list
```

## Agent Inspector

Agent Inspector shows what a local agent does: the requests it receives, the tool calls it makes, the streamed events and the final output. It connects to an agent that already listens on `http://localhost:8088` with the Responses or Invocations protocol, and it detects which one automatically. Opening the inspector does not start your agent.

In VS Code, start the generated debug configuration with F5 and the inspector opens connected. From a terminal, start your agent yourself and launch the inspector.

```bash
azd ai inspector launch
```

The UI is served at `http://127.0.0.1:8087` and targets port 8088 by default. Use `--port` and `--inspector-port` when either port is taken, and `--conversation-id` to replay an existing conversation.

## Foundry MCP server

The Foundry MCP server (preview) is a Microsoft-hosted Model Context Protocol server at `https://mcp.ai.azure.com`. It exposes tools that manage a Foundry project in natural language: agents, sessions, toolboxes, datasets, evaluations, model deployments, connections and more. The documentation lists 79 tools in 17 categories, and the live server returned 80 when this module was written, because the tool set changes during the preview.

Every call runs with your own permissions through the Microsoft Entra on-behalf-of flow. Reader is enough for read tools and Contributor is needed for tools that create, update or delete. Write tools affect live resources and billing at once, so review the parameters before you approve a call.

To use the server from VS Code with GitHub Copilot Agent Mode, add it to `.vscode/mcp.json` in the workspace and start it from the editor.

```json
{
  "servers": {
    "foundry-mcp-remote": {
      "type": "http",
      "url": "https://mcp.ai.azure.com"
    }
  }
}
```

Any MCP client can connect, including your own code. The token must be issued for the audience `https://mcp.ai.azure.com`, and the client then lists and calls tools like with any other MCP server.

```python
token = DefaultAzureCredential().get_token("https://mcp.ai.azure.com/.default").token
http = httpx2.AsyncClient(headers={"Authorization": f"Bearer {token}"})

async with Client(streamable_http_client("https://mcp.ai.azure.com", http_client=http)) as client:
    tools = (await client.list_tools()).tools
    result = await client.call_tool("agent_get", {"projectEndpoint": endpoint})
```

## Limits of the preview

The hosted server uses a public endpoint and cannot reach Foundry resources behind Azure Private Link. Requests may be processed in EU or US data centers, it has no SLA, and tool names or parameters can change without notice.

Use it for development and operations tasks. For private networking, build your own MCP server and connect it to the agent service.

## Hands-On Demo

[Operate Foundry with the MCP Server and Agent Inspector](./demo-foundry-mcp.md) installs the azd extensions, calls the Foundry MCP server from Python to read deployments and to create, invoke and delete a prompt agent, and inspects a local agent with Agent Inspector. The MCP steps run in Python from `foundry-mcp-py/` and in C# from `foundry-mcp-cs/`, and the local agent exists as `foundry-mcp-py/local-agent/` and `local-agent-cs/`.

## Key Topics

- [Foundry Toolkit for Visual Studio Code overview](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/get-started-projects-visual-studio-code)
- [Install Foundry Toolkit for Visual Studio Code](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/install-foundry-toolkit-visual-studio-code)
- [Debug agents with Agent Inspector](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/vs-code-agent-inspector)
- [Install the Azure Developer CLI Foundry extensions](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/install-cli-foundry-extensions)
- [Inspect a local agent with the Agent Inspector](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/agent-inspector)
- [Get started with Foundry MCP Server](https://learn.microsoft.com/en-us/azure/foundry/mcp/get-started)
- [Available tools and example prompts for Foundry MCP Server](https://learn.microsoft.com/en-us/azure/foundry/mcp/available-tools)
- [Foundry MCP Server best practices and security guidance](https://learn.microsoft.com/en-us/azure/foundry/mcp/security-best-practices)
