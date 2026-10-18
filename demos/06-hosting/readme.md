# Module 6: Hosting, Durable Agents & Protocols

An agent that works in a notebook is not yet a service. Someone has to decide where it runs, which identity it runs as, what wakes it up, how it survives a restart while a human thinks about an approval, and how other agents and web front ends talk to it. This module answers those questions for agents built with Agent Framework and for prompt agents in Microsoft Foundry.

You start with the hosting that Foundry runs for you: hosted agents with a dedicated Entra identity, the `azd ai agent` loop that ships and monitors them, and routines that start an agent on a timer, a schedule or a GitHub event. Then you take hosting into your own hands with durable agents on Azure Functions and the Durable Task Scheduler, and with self-hosted endpoints in ASP.NET Core and FastAPI. The last three topics are the protocols that connect agents to each other and to people: A2A with agent cards, enterprise A2A with a registry and shared state, and AG-UI with CopilotKit for web front ends.

Every topic has a Python project. Topics 1 to 8 also have a C# project, because the host, the Functions worker and the A2A and AG-UI servers are first-class .NET code. Publishing agents to Microsoft 365 or Teams is not part of this module.

## Topics

| Topic | Description |
| --- | --- |
| [Hosted Agents: Containers & Agent Identity](./01-hosted-agents/readme.md) | Foundry-run compute for your own code: `ResponsesHostServer`, native workflow hosting, container or ZIP packaging, the dedicated agent identity and the injected settings |
| [Foundry Routines: Schedules & Event Triggers](./02-routines/readme.md) | One trigger and one action per routine: timer, cron schedule and GitHub issue triggers, manual dispatch, run history and the `azure.ai.routines` azd extension |
| [azd AI Agent: Deploy & Monitor](./03-azd-ai-agent/readme.md) | `azd ai agent init`, `run`, `invoke`, `monitor` and `doctor`, and `azd deploy` into an existing project |
| [Durable Agents: Functions & Task Scheduler](./04-durable-agents/readme.md) | `AgentFunctionApp` and the Durable Task Scheduler: persistent sessions, orchestrations and approval waits that cost no compute |
| [Self-Hosting: ASP.NET Core & Responses](./05-self-hosting/readme.md) | `AddAIAgent` and `MapOpenAIResponses` in ASP.NET Core and the Python Responses hosting helpers, with sessions and continuation |
| [A2A Protocol: Agent Cards & Endpoints](./06-a2a-protocol/readme.md) | Agent cards, `A2AExecutor` and `A2AAgent`, .NET `AddA2AServer`, incoming A2A on Foundry agents and the `a2a` tool |
| [Enterprise A2A: Registry & Shared State](./07-enterprise-a2a/readme.md) | A capability registry, ETag-protected shared state, tenant isolation and conflict resolution with an audit trail |
| [AG-UI & CopilotKit: Web Front Ends](./08-ag-ui/readme.md) | AG-UI endpoints for agents and workflows, the event stream, approvals as interrupts and a CopilotKit runtime through an `HttpAgent` |

## Demos

| Topic | Description |
| --- | --- |
| [Deploy a Hosted Agent and Inspect Its Identity](./01-hosted-agents/demo-hosted-agents.md) | Host an agent and a workflow locally, build the container, deploy a ZIP as a hosted agent, call its endpoint and read its Entra identity |
| [Schedule an Agent with Routines](./02-routines/demo-routines.md) | Create a weekday schedule and a one-time timer, dispatch a test run, add the same schedule from an azd manifest and read the run history (`routines-py`, `routines-cs`) |
| [Ship a Hosted Agent with azd](./03-azd-ai-agent/demo-azd-ai-agent.md) | Scaffold an azd project, run the agent locally, deploy it, invoke it, stream its logs and run the doctor check (`azd-ai-agent-py`, `azd-ai-agent-cs`) |
| [Run an Approval Workflow That Survives a Restart](./04-durable-agents/demo-durable-agents.md) | A durable writer agent with a three-round approval orchestration, a host restart during the wait and a deployment to Flex Consumption (`durable-agents-py`, `durable-agents-cs`) |
| [Host an Agent Behind a Responses Endpoint](./05-self-hosting/demo-self-hosting.md) | Serve a support-desk agent from ASP.NET Core and FastAPI, call it with the OpenAI SDK and continue a conversation |
| [Expose and Call Agents over A2A](./06-a2a-protocol/demo-a2a-protocol.md) | Serve two agents over A2A, call and route between them, then expose a Foundry prompt agent and call it through the A2A tool |
| [Build an Enterprise A2A Network](./07-enterprise-a2a/demo-enterprise-a2a.md) | Route by capability and health, write shared state with ETags, isolate tenants and resolve conflicting agent outputs |
| [Stream an Agent to a Web Front End](./08-ag-ui/demo-ag-ui.md) | Read the AG-UI event stream, run an approval round trip, see workflow step events and put a CopilotKit runtime in front |

## Working with the Demo Projects

Each project folder is a finished, runnable project with its own `requirements.txt` or `.csproj` and a `.env.example`. Create a virtual environment inside the project, install the pinned requirements and copy `.env.example` to `.env`. Set `PROJECT_ENDPOINT` to your Foundry project endpoint and `MODEL_DEPLOYMENT_NAME` to a chat model deployment such as `gpt-5.4`, and sign in with `az login`.

```bash
cd demos/06-hosting/01-hosted-agents/hosted-agents-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

Only the azd topic ships a starter and a finished sibling: `azd-ai-agent-py/` and `azd-ai-agent-cs/` hold the agent source, and the `-solution` folders hold the same projects after `azd ai agent init`. The Python hosting libraries (`agent-framework-foundry-hosting`, `agent-framework-hosting` and the A2A packages) are prerelease, so every requirements file pins an exact version. Each Cleanup step removes the project-scoped agents, routines and connections its demo created.

## Helpful Claude Slash Commands

| Command | What it does here |
| --- | --- |
| `/security-review` | Reviews a branch of hosting code for the risks this module raises: unauthenticated endpoints, session or response ids trusted as credentials and role assignments that are broader than the agent needs |
| `/review` | Reviews host code for blocking calls in async handlers, shared agent instances and per-request resources that are never closed |
| `/doctor` | Checks the Claude Code installation and MCP servers before you point an agent at the Azure CLI, `azd` and Docker commands these demos use |
| `/cost` | Shows token usage and session cost, a useful comparison when a durable orchestration replays or an AG-UI client resends a long thread |
| `/find-skills` | Finds installable skills for Azure Functions, Foundry hosting and `azd` workflows |

## Key Topics covered in this module

- [What are hosted agents?](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/hosted-agents)
- [Foundry Hosted Agents with Agent Framework](https://learn.microsoft.com/en-us/agent-framework/hosting/foundry-hosted-agent)
- [Routines in Foundry Agent Service](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/routines)
- [Install the Azure Developer CLI Foundry extensions](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/install-cli-foundry-extensions)
- [Durable Extension for Agent Framework](https://learn.microsoft.com/en-us/agent-framework/hosting/durable-extension)
- [Self-host Agent Framework applications](https://learn.microsoft.com/en-us/agent-framework/hosting/self-hosting/)
- [Enable incoming A2A on a Foundry agent](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/enable-agent-to-agent-endpoint)
- [A2A agent service in Agent Framework](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/agent-services/a2a)
- [AG-UI integration with Agent Framework](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/ui/ag-ui/)
