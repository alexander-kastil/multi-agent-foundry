# A2A Protocol: Agent Cards & Endpoints

Agents built by different teams rarely share a framework. One team ships a Python service, another an ASP.NET Core app, a third a prompt agent in Microsoft Foundry. The Agent2Agent (A2A) protocol gives them one wire format, so any of them can discover and call any other without sharing code.

## What A2A standardizes

A2A defines three things: how an agent describes itself, how a client sends it work, and how long-running work is tracked. The description is the agent card, a JSON document that lists the agent name, version, skills and the interfaces it listens on. The work is a message that the server answers with either a direct reply or a task with a state and artifacts.

A2A is not a model API and not a tool protocol. MCP connects an agent to tools and data; A2A connects an agent to another agent that reasons on its own. The caller sees only the card and the answers, never the remote instructions, tools or model.

## The agent card

A client starts with discovery. A self-hosted agent publishes its card at `/.well-known/agent-card.json`; a client fetches it and learns where to send messages.

```json
{
  "name": "m6h2-title-agent",
  "description": "Writes blog post titles.",
  "supportedInterfaces": [
    { "url": "http://localhost:9101/", "protocolBinding": "JSONRPC" }
  ],
  "version": "1.0.0",
  "skills": [
    { "id": "generate_title", "name": "Generate title", "description": "Writes a blog post title for a topic." }
  ]
}
```

Each entry in `supportedInterfaces` is a URL plus a protocol binding. A2A defines JSON-RPC, HTTP+JSON and gRPC bindings, and a server can offer several at once. Foundry serves its own cards at `agentCard/v1.0` and `agentCard/v0.3` under the agent endpoint, and they require a Microsoft Entra token.

## Protocol versions and status

Foundry Agent Service supports A2A protocol 1.0 (generally available) and 0.3 (preview). The A2A tool follows the same split: the `a2a` tool type is protocol 1.0 and generally available, `a2a_preview` is protocol 0.3 and in preview. New integrations target 1.0.

A Foundry endpoint that receives no version selector serves 0.3 by default. Select 1.0 with the `A2A-Version: 1.0` header, the `?a2a-version=1.0` query string, or by fetching the `agentCard/v1.0` card so the client SDK negotiates it. On a Foundry endpoint, version 1.0 is JSON-RPC only, text only, and without streaming.

## Serving your own agent over A2A

Self-hosting gives you the full A2A surface. In Python, the `agent-framework-a2a` package provides `A2AExecutor`, which adapts an Agent Framework `Agent` to the `a2a-sdk` server. You assemble the card, the request handler, the task store and the routes around it.

```python
card = AgentCard(
    name=agent.name,
    description=description,
    version="1.0.0",
    default_input_modes=["text"],
    default_output_modes=["text"],
    capabilities=AgentCapabilities(streaming=False),
    supported_interfaces=[AgentInterface(url=f"http://localhost:{port}/", protocol_binding="JSONRPC")],
    skills=[skill],
)
handler = DefaultRequestHandler(
    agent_executor=A2AExecutor(agent),
    task_store=InMemoryTaskStore(),
    agent_card=card,
)
app = Starlette(routes=[*create_agent_card_routes(card), *create_jsonrpc_routes(handler, "/")])
```

In ASP.NET Core, the `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` package registers the agent with `AddA2AServer` and maps one route per protocol binding. `MapWellKnownAgentCard` publishes the card.

```csharp
var titleAgent = builder.AddAIAgent("title", instructions: "You write one catchy blog post title for the topic you are given. Reply with the title only.");
titleAgent.AddA2AServer();

app.MapA2AHttpJson(titleAgent, "/a2a/title");
app.MapA2AJsonRpc(titleAgent, "/a2a/title-rpc");
app.MapWellKnownAgentCard(card);
```

## Calling a remote agent

`A2AAgent` wraps a remote A2A endpoint as an ordinary Agent Framework agent. Build it from a resolved card, or from a bare `url=` when the endpoint is known; with a URL only, it creates a minimal card and uses JSON-RPC. The conversation identity travels as the A2A `context_id` stored on the session.

```python
card = await A2ACardResolver(httpx_client=http_client, base_url=url).get_agent_card()
async with A2AAgent(agent_card=card) as agent:
    response = await agent.run("Hosted agents without the plumbing")
```

Because `A2AAgent` is an agent, `as_tool()` turns it into a function tool for another agent. A router agent then delegates to several remote specialists without knowing how any of them is built.

```python
router = Agent(client=client, name="router", instructions="...", tools=[remote.as_tool() for remote in remotes])
```

## A2A in Foundry Agent Service

Foundry supports both directions. For incoming calls, one PATCH on a prompt agent adds an agent card and switches on the A2A protocol next to the responses protocol. The portal cannot do this yet; use the REST API or the SDK.

```python
project.agents.update_details(
    agent_name="m6h2-a2a-agent",
    agent_endpoint=AgentEndpointConfig(
        protocol_configuration=ProtocolConfiguration(
            responses=ResponsesProtocolConfiguration(),
            a2a=A2AProtocolConfiguration(),
        ),
    ),
    agent_card=AgentCard(version="1.0", description="...", skills=[AgentCardSkill(id="releases", name="Release questions", description="...")]),
)
```

Callers authenticate with Microsoft Entra only, and the calling identity needs the Foundry Agent Consumer role on the project or on the single agent. For outgoing calls, a project connection of category `RemoteA2A` stores the target URL and auth, and the `A2ATool` attaches it to a prompt agent.

```python
tools=[A2ATool(a2a_version=A2AProtocolVersion.V1_0, project_connection_id=connection.id)]
```

The Connected Agents tool of the classic Agents API is not part of the new Foundry Agent Service. The A2A tool replaces it, and it also reaches agents that run outside Foundry.

## Choosing between self-hosted and Foundry-hosted A2A

| Question | Self-hosted A2A server | Foundry incoming A2A |
| --- | --- | --- |
| Who runs it | Your container or service | Foundry Agent Service |
| Card location | `/.well-known/agent-card.json` | `.../endpoint/protocols/a2a/agentCard/v1.0` |
| Auth | Yours to implement | Microsoft Entra, Foundry Agent Consumer role |
| Transports | Whatever you map | JSON-RPC for v1.0 |
| Streaming and files | Supported by the SDK | Text only, no streaming |

## Hands-On Demo

[Expose and Call Agents over A2A](demo-a2a-protocol.md) serves two Agent Framework agents over A2A, calls them with `A2AAgent`, routes between them, then exposes a Foundry prompt agent and has a second Foundry agent call it through the A2A tool.

## Key Topics

- [Enable incoming A2A on a Foundry agent](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/enable-agent-to-agent-endpoint)
- [Connect to an A2A agent endpoint from Foundry Agent Service](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/agent-to-agent)
- [Host agents with A2A (Agent Framework)](https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/a2a/server)
- [A2A agent service (Agent Framework client)](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/agent-services/a2a)
- [Self-host Agent Framework applications](https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/)
