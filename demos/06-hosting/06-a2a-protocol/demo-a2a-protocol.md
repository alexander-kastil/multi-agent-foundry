# Expose and Call Agents over A2A

Agents that only talk to their own framework cannot be reused by anyone else. In this demo you publish two agents over the A2A protocol, call them from a third agent, expose a Foundry prompt agent the same way, and let a second Foundry agent call it.

## Prerequisites

- Python 3.12 or later, .NET 10 SDK, and `az login` on the subscription that holds the class Foundry project.
- A Foundry project with a chat deployment, for example `gpt-5.4`, and the Foundry Project Manager role on it.
- Concepts: [A2A Protocol: Agent Cards & Endpoints](readme.md).

## Run it

```bash
cd a2a-protocol-py
python -m venv .venv
.venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
```

Fill `.env` with the project endpoint, the model deployment, and the subscription, resource group, account and project names. Start the two A2A servers, each in its own terminal.

```bash
python title_server.py
```

```bash
python outline_server.py
```

## Step 1: Serve an agent over A2A

`a2a_host.py` wraps any Agent Framework agent in `A2AExecutor` and builds the Starlette app around it. The agent card is the contract: name, skills and the JSON-RPC interface URL.

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

Fetch the card from a third terminal.

```bash
curl http://localhost:9101/.well-known/agent-card.json
```

Expected output (trimmed):

```text
{"name":"m6h2-title-agent","description":"Writes blog post titles.","supportedInterfaces":[{"url":"http://localhost:9101/","protocolBinding":"JSONRPC"}],"version":"1.0.0","capabilities":{"streaming":false},"defaultInputModes":["text"],"defaultOutputModes":["text"],"skills":[{"id":"generate_title","name":"Generate title","description":"Writes a blog post title for a topic.","tags":["title","blog"]}]}
```

The same server in C# lives in `a2a-protocol-cs`. It registers the agent, maps an HTTP+JSON and a JSON-RPC route, and publishes the card.

```csharp
var titleAgent = builder.AddAIAgent(
    "title",
    instructions: "You write one catchy blog post title for the topic you are given. Reply with the title only.");
titleAgent.AddA2AServer();

var app = builder.Build();

app.MapA2AHttpJson(titleAgent, "/a2a/title");
app.MapA2AJsonRpc(titleAgent, "/a2a/title-rpc");
```

Run it with the project values as environment variables, then post a message to the HTTP+JSON route.

```bash
cd a2a-protocol-cs
PROJECT_ENDPOINT=<endpoint> MODEL_DEPLOYMENT_NAME=gpt-5.4 ASPNETCORE_URLS=http://localhost:5000 dotnet run
```

```bash
curl -s -X POST http://localhost:5000/a2a/title/message:send -H "Content-Type: application/json" -d '{"message":{"role":"ROLE_USER","parts":[{"text":"Hosted agents without the plumbing"}],"messageId":"m1","contextId":"c1"}}'
```

Expected output:

```text
{"message":{"role":"ROLE_AGENT","parts":[{"text":"Hosted Agents Without the Plumbing: Build Smarter, Ship Faster"}],"messageId":"resp_0cacfc4413...","contextId":"c1"}}
```

The C# card lists both interfaces with protocol version 1.0.

```text
"supportedInterfaces":[{"url":"http://localhost:5000/a2a/title","protocolBinding":"HTTP+JSON","protocolVersion":"1.0"},{"url":"http://localhost:5000/a2a/title-rpc","protocolBinding":"JSONRPC","protocolVersion":"1.0"}]
```

Stop the C# server with Ctrl+C before moving on.

## Step 2: Call the agent with A2AAgent

`client.py` resolves the card and wraps it as an `A2AAgent`. From here the remote agent is called like any local one.

```python
async with httpx.AsyncClient(timeout=60.0) as http_client:
    card = await A2ACardResolver(httpx_client=http_client, base_url=TITLE_URL).get_agent_card()
print(f"card: {card.name} skills={[skill.id for skill in card.skills]}")

async with A2AAgent(agent_card=card) as agent:
    response = await agent.run("Hosted agents without the plumbing")
    print(f"title: {response.messages[0].text}")
```

```bash
python client.py
```

Expected output:

```text
card: m6h2-title-agent skills=['generate_title']
title: Hosted Agents Without the Plumbing: Build Smarter, Ship Faster
```

## Step 3: Route between two remote agents

A router agent gets each remote agent as a tool through `as_tool`. It only knows names and descriptions from the cards, so adding a third specialist is a new URL in the list.

```python
router = Agent(
    client=FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    ),
    name="m6h2-routing-agent",
    instructions=(
        "You route requests to remote agents. Ask the title agent for a title first, "
        "then pass that title to the outline agent, and return both."
    ),
    tools=[remote.as_tool(name=remote.name.replace("-", "_"), description=remote.description) for remote in remotes],
)
```

```bash
python routing_client.py
```

Expected output (the wording varies per run):

```text
found remote agent: m6h2-title-agent
found remote agent: m6h2-outline-agent
**Title:** Hosted Agents in Microsoft Foundry: Build Smarter AI Without the Infrastructure Headache

**Outline:**
1. What Hosted Agents Are in Microsoft Foundry
2. Why Teams Choose Managed AI Infrastructure
3. Core Features That Simplify Agent Development
4. Building Smarter Workflows With Hosted Agents
5. Security, Scalability, and Operational Benefits
6. Getting Started With Microsoft Foundry Hosted Agents
```

Stop both A2A servers with Ctrl+C.

## Step 4: Expose a Foundry prompt agent over A2A

Foundry can host the A2A endpoint for you. `enable_incoming.py` creates a prompt agent, then patches it with an agent card and the A2A protocol next to the responses protocol.

```python
patched = project.agents.update_details(
    agent_name="m6h2-a2a-agent",
    agent_endpoint=AgentEndpointConfig(
        protocol_configuration=ProtocolConfiguration(
            responses=ResponsesProtocolConfiguration(),
            a2a=A2AProtocolConfiguration(),
        ),
    ),
    agent_card=AgentCard(
        version="1.0",
        description="Answers questions about Contoso product releases.",
        skills=[AgentCardSkill(id="releases", name="Release questions", description="Explains what shipped and when.")],
    ),
)
```

```bash
python enable_incoming.py
python call_foundry_a2a.py
```

`call_foundry_a2a.py` sends a bearer token for `https://ai.azure.com/.default`, resolves the card from `agentCard/v1.0` and sends a v1.0 message. Expected output (trimmed):

```text
agent m6h2-a2a-agent version 1
incoming A2A enabled for m6h2-a2a-agent
card: m6h2-a2a-agent version=1.0 skills=['releases']
{
  "task": {
    "id": "resp_08d54642c6de696c...",
    "contextId": "ctxt_9b2b3d4ecab347ad99c13856d465d573",
    "status": {
      "state": "TASK_STATE_COMPLETED",
      ...
    },
    "artifacts": [
      {
        "parts": [
          {
            "text": "The release desk covers product release planning, readiness, and coordination across teams. It also helps answer questions about timelines, milestones, risks, and launch status."
          }
        ]
      }
    ]
  }
}
```

## Step 5: Let a Foundry agent call it with the A2A tool

The outgoing direction needs a project connection of category `RemoteA2A` that points at the base path, and an agent that carries the `A2ATool`. `create_a2a_caller.py` creates both and prints the caller identity.

```python
caller = project.agents.create_version(
    agent_name="m6h2-a2a-caller",
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions="You are a helpful assistant. Use the A2A tool to delegate release questions to the release desk.",
        tools=[A2ATool(a2a_version=A2AProtocolVersion.V1_0, project_connection_id=connection.id)],
    ),
)
```

```bash
python create_a2a_caller.py
```

Expected output:

```text
connection m6h2-a2a-target created
agent m6h2-a2a-caller version 2
caller identity: a4b739d3-eaac-4dc3-afb4-4fbce0da237f
```

The caller reaches the target with its own agent identity, so that identity needs the Foundry Agent Consumer role on the target agent. Use the printed principal id.

```bash
az role assignment create --assignee-object-id <caller identity> --assignee-principal-type ServicePrincipal --role eed3b665-ab3a-47b6-8f48-c9382fb1dad6 --scope /subscriptions/<subscription>/resourceGroups/<resource group>/providers/Microsoft.CognitiveServices/accounts/<account>/projects/<project>/agents/m6h2-a2a-agent
```

Wait a minute for the assignment to apply, then ask the caller. `tool_choice="required"` forces the tool call.

```bash
python ask_a2a_caller.py
```

Expected output (the wording varies per run):

```text
The release desk covers planning, coordination, and communication for product releases across Contoso teams. It helps with release schedules, readiness checks, dependencies, and go-live support.
['a2a_preview_call', 'a2a_preview_call_output', 'message']
```

The second line lists the response output items; the delegation shows up as the two call items before the final message. Even with `A2AProtocolVersion.V1_0` the call items were reported with the `a2a_preview_call` type on the day this guide was run.

## Verify

The release answer in Step 5 came from `m6h2-a2a-agent`, not from the caller model, and the output items prove the tool call happened. In Step 3 both remote agents appear in the found-remote-agent lines and one answer carries both a title and an outline.

## Cleanup

`cleanup.py` deletes both Foundry agents and the `m6h2-a2a-target` connection. Remove the role assignment with the same object id and scope, and stop any server still running.

```bash
python cleanup.py
az role assignment delete --assignee <caller identity> --role eed3b665-ab3a-47b6-8f48-c9382fb1dad6 --scope /subscriptions/<subscription>/resourceGroups/<resource group>/providers/Microsoft.CognitiveServices/accounts/<account>/projects/<project>/agents/m6h2-a2a-agent
```

The shipped C# project covers the self-hosted server of Step 1. Steps 2 to 5 are shown in Python only; the Foundry-side calls have C# equivalents in the linked Microsoft Learn pages.
