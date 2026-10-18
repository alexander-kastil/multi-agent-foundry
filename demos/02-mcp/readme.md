# Module 2: MCP Servers, Security & Hosting

Agents become useful when they can reach your data and your systems, and the Model Context Protocol (MCP) is the common contract for that. This module takes a tool server from first line of code to a hosted, secured and shared endpoint that Microsoft Foundry agents consume. You build servers in C# on `ModelContextProtocol` 2.2.0 and in Python on `mcp` 2.3.0, debug them with the MCP Inspector, protect them with Microsoft Entra ID, host them on Azure Functions, harden them against backend failures and publish them once through a Foundry toolbox.

Every demo is a finished, run-verified project in the topic folder, and the guide walks through its code. The Python projects pin their own packages. Agent Framework 1.22.0 cannot yet read the initialize result of `mcp` 2.x, so the projects that connect an agent to a server use `mcp==1.30.0` in a separate environment from the servers that use `mcp==2.3.0`.

| Topic | Description |
| --- | --- |
| [01 MCP Servers: Tools, Resources & Prompts](./01-mcp-servers/readme.md) | The three server primitives, tool design, and the same HR server in C# and Python |
| [02 Transports & MCP Inspector Debugging](./02-transports/readme.md) | stdio versus Streamable HTTP, the two protocol eras on the wire, and debugging with the Inspector |
| [03 MCP Authorization: OAuth 2.1 & PRM](./03-mcp-authorization/readme.md) | Protected Resource Metadata, resource indicators, scopes and step-up with Entra ID, and how Foundry agents authenticate |
| [04 Hosting MCP on Azure Functions](./04-azure-functions/readme.md) | The Functions MCP extension, keys, deployment to Flex Consumption and a Foundry agent connection |
| [05 MCP Resilience: Retry & Circuit Breaker](./05-mcp-resilience/readme.md) | Retry with backoff, a circuit breaker, fallback results and routing in Python and C# |
| [06 Foundry Toolbox: Managed MCP Endpoints](./06-foundry-toolbox/readme.md) | One managed endpoint per toolbox, versions and promotion, and consumption from Agent Framework in Python and .NET |

| Topic | Description |
| --- | --- |
| [Build and Test an HR MCP Server in C# and Python](./01-mcp-servers/demo-mcp-servers.md) | Run the server, inspect tools, resources and prompts, and let a Foundry model call it |
| [Compare stdio and Streamable HTTP with the MCP Inspector](./02-transports/demo-transports.md) | Run one server over both transports and read the raw messages of both protocol eras |
| [Secure an MCP Server with Microsoft Entra ID](./03-mcp-authorization/demo-mcp-authorization.md) | Register an API, validate tokens in ASP.NET Core and Python, and complete a scope step-up |
| [Host an MCP Server on Azure Functions and Call It from a Foundry Agent](./04-azure-functions/demo-azure-functions.md) | Deploy to Flex Consumption, protect it with the system key and connect a prompt agent |
| [Make an MCP Server Survive a Backend Outage](./05-mcp-resilience/demo-mcp-resilience.md) | Force an outage and watch retries, the breaker and the fallback, then add an agent |
| [Publish Tools Once with a Foundry Toolbox](./06-foundry-toolbox/demo-foundry-toolbox.md) | Create, version and consume a toolbox from Python and .NET |

## Helpful Claude Slash Commands

| Command | Usage |
| --- | --- |
| `/mcp` | List the MCP servers Claude Code knows, authenticate against a remote server and reconnect one that failed |
| `/doctor` | Check the installation and the health of configured MCP servers |
| `/security-review` | Review a branch for missing token validation, hard-coded keys and unsafe tool exposure |
| `/update-config` | Add an MCP server or its permissions to `settings.json` |

Use `claude mcp add --transport http <name> <url>` to register a remote server you built in this module, and `/mcp` to sign in when it needs OAuth.

## Key Topics covered in this module

- [Get started with .NET AI and the Model Context Protocol](https://learn.microsoft.com/dotnet/ai/get-started-mcp)
- [Using MCP tools with agents (Agent Framework)](https://learn.microsoft.com/agent-framework/agents/tools/local-mcp-tools)
- [Secure a Model Context Protocol (MCP) server with Microsoft Entra ID](https://learn.microsoft.com/entra/agent-id/secure-mcp-server-with-entra-id)
- [Set up authentication for MCP tools in Foundry Agent Service](https://learn.microsoft.com/azure/foundry/agents/how-to/mcp-authentication)
- [Connect an MCP server on Azure Functions to a Foundry Agent Service agent](https://learn.microsoft.com/azure/azure-functions/functions-mcp-foundry-tools)
- [Model Context Protocol bindings for Azure Functions overview](https://learn.microsoft.com/azure/azure-functions/functions-bindings-mcp)
- [What is Toolbox in Foundry?](https://learn.microsoft.com/azure/foundry/agents/concepts/toolbox-overview)
- [Create and manage a toolbox in Foundry](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/toolbox)
