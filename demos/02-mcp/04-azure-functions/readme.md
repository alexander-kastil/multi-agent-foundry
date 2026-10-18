# Hosting MCP on Azure Functions

A Foundry agent can only call a remote MCP server, so the server you built on your laptop has to run somewhere with a public or reachable HTTPS endpoint. Azure Functions is one of the two hosting options Microsoft documents for this, next to Azure Container Apps. A function app scales to zero, takes care of TLS and keys, and the MCP extension turns individual functions into MCP tools without a server framework in your code.

This topic shows the extension model in Python and C#, runs it locally, deploys it to a Flex Consumption function app and connects a Foundry agent to it with a key stored in a project connection.

## Functions or Container Apps

The choice depends on how much of the MCP server you already wrote.

| Question | Azure Functions | Azure Container Apps |
| --- | --- | --- |
| How do you ship code? | Functions project with MCP trigger functions | Container image of any MCP server |
| Languages | Python, Node.js, TypeScript, Java, .NET | Anything that runs in a Linux container |
| Transport | Streamable HTTP, built in | Streamable HTTP or other HTTP endpoints you implement |
| Authentication | Function keys or built-in authentication | You implement it |
| State | Stateless only | Stateless only |
| Not supported | Containerized servers, `npx` start commands, OS-level dependencies such as Playwright | Privileged containers, non-amd64 images |

An existing server written with the MCP SDK fits a container. A set of focused tools that call Azure services fits the Functions extension.

## The MCP Extension Model

The extension adds triggers to Azure Functions. A tool trigger exposes a function as an MCP tool, and there are also resource and prompt triggers. The host speaks MCP for you, answers `tools/list` from the function signatures and runs the matching function for each `tools/call`.

In Python, the `azure-functions` package (1.24 or later) provides the `mcp_tool` decorator. The function name becomes the tool name, the docstring becomes the description, and the parameters become the input schema.

```python
import azure.functions as func

app = func.FunctionApp()

BLOB_PATH = "snippets/{mcptoolargs.snippetname}.json"


@app.mcp_tool()
def hello_mcp() -> str:
    """Says hello from an MCP server hosted on Azure Functions."""
    return "Hello I am MCPTool!"


@app.mcp_tool()
@app.mcp_tool_property(arg_name="snippetname", description="The name of the snippet.")
@app.blob_input(arg_name="file", connection="AzureWebJobsStorage", path=BLOB_PATH)
def get_snippet(file: func.InputStream, snippetname: str) -> str:
    """Retrieves a snippet by name from blob storage."""
    return file.read().decode("utf-8")
```

The blob path shows the main benefit of the model. `{mcptoolargs.snippetname}` binds a tool argument into a normal Functions binding, so the function receives the blob content without any storage code. Without the `mcptoolargs.` prefix the binding fails with `No value for named parameter`.

In C#, the isolated worker model uses `McpToolTrigger` and `McpToolProperty`. Only the isolated worker model is supported.

```csharp
[Function(nameof(SaveSnippet))]
[BlobOutput(BlobPath)]
public string SaveSnippet(
    [McpToolTrigger("save_snippet", "Saves a snippet under a name into blob storage.")] ToolInvocationContext context,
    [McpToolProperty("snippetname", "The name of the snippet.", isRequired: true)] string name,
    [McpToolProperty("snippet", "The content of the snippet.", isRequired: true)] string snippet) => snippet;
```

Properties are optional unless you mark them required. The C# package is `Microsoft.Azure.Functions.Worker.Extensions.Mcp`.

## Configuration

The extension ships in the extension bundle, so `host.json` references the bundle and optionally describes the server.

```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[4.0.0, 5.0.0)"
  },
  "extensions": {
    "mcp": {
      "serverName": "m2-snippets",
      "serverVersion": "1.0.0",
      "instructions": "Save and retrieve named code snippets."
    }
  }
}
```

`instructions` describes to clients how to use the remote server. A C# app needs no bundle because the NuGet package carries the extension.

## Endpoint and Keys

The extension serves Streamable HTTP at `/runtime/webhooks/mcp` and the legacy SSE transport at `/runtime/webhooks/mcp/sse`. Unless a client needs SSE, use Streamable HTTP.

In Azure the endpoint requires the system key named `mcp_extension`, sent in the `x-functions-key` header or the `code` query parameter. Without it the host answers `401`.

```bash
az functionapp keys list --resource-group <resource-group> --name <app-name> --query systemKeys.mcp_extension --output tsv
```

Setting `system.webhookAuthorizationLevel` to `Anonymous` in `host.json` removes the key requirement. You then protect the endpoint with the built-in MCP authorization of App Service, which is in preview, or with your own check.

## Run It Locally

Core Tools 4.0.7030 or later runs the extension. The host needs a storage account, so a local run uses the Azurite emulator, and Python needs one extra setting.

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "python",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "PYTHON_ISOLATE_WORKER_DEPENDENCIES": "1"
  }
}
```

With Core Tools 4.5.0 the Python worker loads its bundled copy of `azure.functions` unless `PYTHON_ISOLATE_WORKER_DEPENDENCIES` is `1`, and the bundled copy has no `mcp_tool` decorator. The host then fails to index the app with `'FunctionApp' object has no attribute 'mcp_tool'`.

## Connecting a Foundry Agent

Foundry Agent Service offers four ways to authenticate to a Functions-hosted MCP server.

| Method | How the agent authenticates | Functions support |
| --- | --- | --- |
| Key-based | Passes the function key in a request header | Yes, the default |
| Microsoft Entra | Agent identity or project managed identity | Project managed identity only |
| OAuth identity passthrough | The signed-in user, through your Entra app registration | Yes, with built-in MCP authentication |
| Unauthenticated | None | Yes, after you disable the key requirement |

For key-based access, the key lives in a project connection of category `RemoteTool` with the custom key `x-functions-key`. The agent references the connection by name, and Agent Service attaches the header on every call.

```python
functions_tool = MCPTool(
    server_label="snippets",
    server_url="https://<app-name>.azurewebsites.net/runtime/webhooks/mcp",
    project_connection_id="m2-func-mcp-key",
    require_approval="never",
)

agent = project.agents.create_version(
    agent_name="m2-functions-mcp-agent",
    definition=PromptAgentDefinition(model="gpt-5.4", instructions="You manage code snippets.", tools=[functions_tool]),
)
```

Anyone with access to the project can read a key stored in a connection, so use it only for shared secrets and prefer Microsoft Entra authentication for production. The Learn article on connecting a Functions MCP server lists the Entra and OAuth setup steps, including the built-in authentication that also switches the key requirement off.

## Hands-On Demo

- [Host an MCP Server on Azure Functions and Call It from a Foundry Agent](./demo-azure-functions.md)

## Key Topics

- [Connect an MCP server on Azure Functions to a Foundry Agent Service agent](https://learn.microsoft.com/azure/azure-functions/functions-mcp-foundry-tools)
- [Model Context Protocol bindings for Azure Functions overview](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp)
- [MCP tool trigger for Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp-tool-trigger)
- [Quickstart: Build a custom remote MCP server using Azure Functions](https://learn.microsoft.com/azure/azure-functions/scenario-custom-remote-mcp-server)
- [Set up authentication for MCP tools in Foundry Agent Service](https://learn.microsoft.com/azure/foundry/agents/how-to/mcp-authentication)
