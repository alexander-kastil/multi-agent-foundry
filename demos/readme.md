# Implementing Multi-Agent Solutions using Microsoft Foundry & Microsoft Agent Framework

This four-day class is for software architects and engineers who build, orchestrate and operate agentic applications with Microsoft Foundry and the Microsoft Agent Framework. The class runs on a core path of 20 topics: each core topic is taught with a runnable demo in Python and C#, followed by a hands-on lab. Topics marked optional form the extended catalog, ship with the same demos and can replace a core topic one for one in a tailored delivery.

Day one works at the Responses API level, from conversations and streaming to context budgets, compaction and prompt caching, then builds the MCP servers the agents call: secured with OAuth 2.1 and hosted on Azure Functions. Day two covers prompt agents in the Foundry Agent Service with built-in tools and Foundry IQ knowledge, then moves to code-first agents in the Agent Framework with chat clients, function tools and approvals.

Day three adds Agent Skills, composes agents into workflows with checkpoints, human-in-the-loop requests and the sequential, concurrent and handoff patterns, and deploys them as hosted agents. The last day makes agents measurable and safe: tracing, agent evaluators, continuous evaluation and red teaming, then guardrails and Entra Agent ID.

The extended catalog goes deeper in every module: model router and the Foundry Toolkit, MCP resilience and the Foundry Toolbox, computer use, memory and voice agents, middleware and the harness agent, Magentic and declarative workflows, durable agents, Foundry Routines, A2A and AG-UI, the Agent Optimizer and incident response, zero trust, CI/CD quality gates and agent lifecycle governance.

## Duration

4 Days

## Audience

- Software Architects & Engineers for Agentic AI Solutions
- AI Pro-Code Developers

## Prerequisites & Requirements

- Python, C#, Typescript
- GitHub Account
- Azure Subscription with access to Microsoft Foundry
- A Foundry project with model deployments, set up before the class as described in [Microsoft Foundry: Projects & Resources](./01-foundry-essentials/01-foundry-projects/)
- Azure Development Skills helpful

## Modules

## Module 1: Foundry Essentials for Agent Developers

- [Microsoft Foundry: Projects & Resources](./01-foundry-essentials/01-foundry-projects/) (optional)
- [Model Deployments & Model Router](./01-foundry-essentials/02-model-router/) (optional)
- [Foundry SDK & Microsoft.Extensions.AI](./01-foundry-essentials/03-foundry-sdk/) (optional)
- [Responses API: Conversations & Streaming](./01-foundry-essentials/04-responses-api/)
- [Foundry Toolkit & Foundry MCP Server](./01-foundry-essentials/05-foundry-toolkit/) (optional)
- [Context Engineering: Budgets, Compaction & Caching](./01-foundry-essentials/06-context-engineering/)

## Module 2: MCP Servers, Security & Hosting

- [MCP Servers: Tools, Resources & Prompts](./02-mcp/01-mcp-servers/)
- [Transports & MCP Inspector Debugging](./02-mcp/02-transports/) (optional)
- [MCP Authorization: OAuth 2.1 & PRM](./02-mcp/03-mcp-authorization/)
- [Hosting MCP on Azure Functions](./02-mcp/04-azure-functions/)
- [MCP Resilience: Retry & Circuit Breaker](./02-mcp/05-mcp-resilience/) (optional)
- [Foundry Toolbox: Managed MCP Endpoints](./02-mcp/06-foundry-toolbox/) (optional)

## Module 3: Foundry Agent Service: Tools, Knowledge & Memory

- [Prompt Agents: Versions & Conversations](./03-agent-service/01-prompt-agents/)
- [Code Interpreter, File & Web Search](./03-agent-service/02-builtin-tools/)
- [Browser Automation & Computer Use](./03-agent-service/03-browser-automation/) (optional)
- [Foundry IQ: Knowledge Bases & Retrieval](./03-agent-service/04-foundry-iq/)
- [Advanced RAG: Hybrid Search & Reranking](./03-agent-service/05-advanced-rag/) (optional)
- [Agent Memory: User & Procedural Memory](./03-agent-service/06-agent-memory/) (optional)
- [Stateful Agentic Loops: Plan & Resume](./03-agent-service/07-agentic-loops/) (optional)
- [Voice Agents: Voice Live API](./03-agent-service/08-voice-agents/) (optional)

## Module 4: Agent Framework: Agents, Tools & Skills

- [Chat Clients: Foundry, OpenAI & Claude](./04-agent-framework/01-chat-clients/)
- [AgentSession: State & Persistence](./04-agent-framework/02-agent-session/) (optional)
- [Context Providers & Long-Term Memory](./04-agent-framework/03-context-providers/) (optional)
- [Middleware: Agent, Function & Chat](./04-agent-framework/04-middleware/) (optional)
- [Function Tools, MCP & Approvals](./04-agent-framework/05-tools-approvals/)
- [Agent Skills: SKILL.md & Progressive Disclosure](./04-agent-framework/06-agent-skills/)
- [Harness Agent: Plan, Execute & Compaction](./04-agent-framework/07-harness-agent/) (optional)

## Module 5: Workflows & Multi-Agent Orchestration

- [Workflows: Executors, Edges & Supersteps](./05-orchestration/01-workflows/)
- [Checkpointing & Human-in-the-Loop Requests](./05-orchestration/02-checkpoints-hitl/)
- [Sequential, Concurrent & Handoff Orchestration](./05-orchestration/03-orchestration-patterns/)
- [Group Chat & Magentic Orchestration](./05-orchestration/04-magentic/) (optional)
- [Task Decomposition: Planner & Waves](./05-orchestration/05-task-decomposition/) (optional)
- [Resilient Orchestration: Quorum & Supervisors](./05-orchestration/06-resilient-orchestration/) (optional)
- [Declarative YAML & Functional Workflows](./05-orchestration/07-declarative-workflows/) (optional)

## Module 6: Hosting, Durable Agents & Protocols

- [Hosted Agents: Containers & Agent Identity](./06-hosting/01-hosted-agents/)
- [Foundry Routines: Schedules & Event Triggers](./06-hosting/02-routines/) (optional)
- [azd AI Agent: Deploy & Monitor](./06-hosting/03-azd-ai-agent/) (optional)
- [Durable Agents: Functions & Task Scheduler](./06-hosting/04-durable-agents/) (optional)
- [Self-Hosting: ASP.NET Core & Responses](./06-hosting/05-self-hosting/) (optional)
- [A2A Protocol: Agent Cards & Endpoints](./06-hosting/06-a2a-protocol/) (optional)
- [Enterprise A2A: Registry & Shared State](./06-hosting/07-enterprise-a2a/) (optional)
- [AG-UI & CopilotKit: Web Front Ends](./06-hosting/08-ag-ui/) (optional)

## Module 7: Evaluation, Observability & Optimization

- [Tracing: OpenTelemetry & Application Insights](./07-evaluation/01-tracing/)
- [Agent Evaluators: Task Adherence & Tools](./07-evaluation/02-agent-evaluators/)
- [Evaluation Suites: Judges & Baselines](./07-evaluation/03-evaluation-suites/) (optional)
- [Continuous Evaluation & AI Red Teaming](./07-evaluation/04-continuous-evaluation/)
- [Agent Optimizer: Instructions & Tool Tuning](./07-evaluation/05-agent-optimizer/) (optional)
- [Performance & Cost: Routing & Caching](./07-evaluation/06-performance-cost/) (optional)
- [Incident Response: Replay & Postmortem](./07-evaluation/07-incident-response/) (optional)

## Module 8: Security, Governance & Delivery

- [Guardrails: Prompt Shields & Intervention Points](./08-governance/01-guardrails/)
- [Prompt Injection: Layered Defenses](./08-governance/02-prompt-injection/) (optional)
- [Entra Agent ID: On-Behalf-Of Flows](./08-governance/03-agent-identity/)
- [Zero Trust: Tenant Isolation & Secrets](./08-governance/04-zero-trust/) (optional)
- [CI/CD: GitHub Actions Quality Gates](./08-governance/05-cicd-gates/) (optional)
- [Agent Lifecycle: Versions, Quotas & Retirement](./08-governance/06-agent-lifecycle/) (optional)
- [Responsible AI: Fairness Probes & Audit](./08-governance/07-responsible-ai/) (optional)

## Schedule

Every core topic is a 70-minute unit: 30 minutes of concept and demo, then a 40-minute hands-on lab. Optional topics are not scheduled; each one is a unit of the same length and replaces a core topic one for one in a tailored delivery.

| Day | Clock          | Length | Deliverable                                                                                                  | Description                                                                                                               |
| --- | -------------- | ------ | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| 1   | 09:00 to 09:10 | 10m    | Welcome                                                                                                      | Introductions and a check of the Foundry project, model deployments and tooling                                           |
| 1   | 09:10 to 10:20 | 1h 10m | [Responses API: Conversations & Streaming](01-foundry-essentials/04-responses-api/readme.md)                 | Demo and lab: responses without an agent resource, `previous_response_id`, conversations and the streaming event sequence |
| 1   | 10:20 to 10:30 | 10m    | Break                                                                                                        |                                                                                                                           |
| 1   | 10:30 to 11:40 | 1h 10m | [Context Engineering: Budgets, Compaction & Caching](01-foundry-essentials/06-context-engineering/readme.md) | Demo and lab: the token budget, trimming by turn, Responses compaction and prompt caching                                 |
| 1   | 11:40 to 12:40 | 1h 00m | Lunch                                                                                                        |                                                                                                                           |
| 1   | 12:40 to 13:50 | 1h 10m | [MCP Servers: Tools, Resources & Prompts](02-mcp/01-mcp-servers/readme.md)                                   | Demo and lab: an MCP server with tools, resources and prompts                                                             |
| 1   | 13:50 to 14:00 | 10m    | Break                                                                                                        |                                                                                                                           |
| 1   | 14:00 to 15:10 | 1h 10m | [MCP Authorization: OAuth 2.1 & PRM](02-mcp/03-mcp-authorization/readme.md)                                  | Demo and lab: OAuth 2.1 and protected resource metadata for an MCP server                                                 |
| 1   | 15:10 to 15:20 | 10m    | Break                                                                                                        |                                                                                                                           |
| 1   | 15:20 to 16:30 | 1h 10m | [Hosting MCP on Azure Functions](02-mcp/04-azure-functions/readme.md)                                        | Demo and lab: an MCP server hosted on Azure Functions                                                                     |
| 2   | 09:00 to 09:10 | 10m    | Recap                                                                                                        | Questions from day one                                                                                                    |
| 2   | 09:10 to 10:20 | 1h 10m | [Prompt Agents: Versions & Conversations](03-agent-service/01-prompt-agents/readme.md)                       | Demo and lab: prompt agents in the Foundry Agent Service with versions and conversations                                  |
| 2   | 10:20 to 10:30 | 10m    | Break                                                                                                        |                                                                                                                           |
| 2   | 10:30 to 11:40 | 1h 10m | [Code Interpreter, File & Web Search](03-agent-service/02-builtin-tools/readme.md)                           | Demo and lab: built-in tools for code execution, file search and web search                                               |
| 2   | 11:40 to 12:40 | 1h 00m | Lunch                                                                                                        |                                                                                                                           |
| 2   | 12:40 to 13:50 | 1h 10m | [Foundry IQ: Knowledge Bases & Retrieval](03-agent-service/04-foundry-iq/readme.md)                          | Demo and lab: knowledge bases and agentic retrieval with Foundry IQ                                                       |
| 2   | 13:50 to 14:00 | 10m    | Break                                                                                                        |                                                                                                                           |
| 2   | 14:00 to 15:10 | 1h 10m | [Chat Clients: Foundry, OpenAI & Claude](04-agent-framework/01-chat-clients/readme.md)                       | Demo and lab: Agent Framework agents on Foundry, OpenAI and Claude chat clients                                           |
| 2   | 15:10 to 15:20 | 10m    | Break                                                                                                        |                                                                                                                           |
| 2   | 15:20 to 16:30 | 1h 10m | [Function Tools, MCP & Approvals](04-agent-framework/05-tools-approvals/readme.md)                           | Demo and lab: function tools, MCP tools and approvals                                                                     |
| 3   | 09:00 to 09:10 | 10m    | Recap                                                                                                        | Questions from day two                                                                                                    |
| 3   | 09:10 to 10:20 | 1h 10m | [Agent Skills: SKILL.md & Progressive Disclosure](04-agent-framework/06-agent-skills/readme.md)              | Demo and lab: Agent Skills with SKILL.md and progressive disclosure                                                       |
| 3   | 10:20 to 10:30 | 10m    | Break                                                                                                        |                                                                                                                           |
| 3   | 10:30 to 11:40 | 1h 10m | [Workflows: Executors, Edges & Supersteps](05-orchestration/01-workflows/readme.md)                          | Demo and lab: workflows with executors, edges and supersteps                                                              |
| 3   | 11:40 to 12:40 | 1h 00m | Lunch                                                                                                        |                                                                                                                           |
| 3   | 12:40 to 13:50 | 1h 10m | [Checkpointing & Human-in-the-Loop Requests](05-orchestration/02-checkpoints-hitl/readme.md)                 | Demo and lab: checkpoints and human-in-the-loop requests in a workflow                                                    |
| 3   | 13:50 to 14:00 | 10m    | Break                                                                                                        |                                                                                                                           |
| 3   | 14:00 to 15:10 | 1h 10m | [Sequential, Concurrent & Handoff Orchestration](05-orchestration/03-orchestration-patterns/readme.md)       | Demo and lab: sequential, concurrent and handoff orchestration                                                            |
| 3   | 15:10 to 15:20 | 10m    | Break                                                                                                        |                                                                                                                           |
| 3   | 15:20 to 16:30 | 1h 10m | [Hosted Agents: Containers & Agent Identity](06-hosting/01-hosted-agents/readme.md)                          | Demo and lab: hosted agents with containers and agent identity                                                            |
| 4   | 09:00 to 09:10 | 10m    | Recap                                                                                                        | Questions from day three                                                                                                  |
| 4   | 09:10 to 10:20 | 1h 10m | [Tracing: OpenTelemetry & Application Insights](07-evaluation/01-tracing/readme.md)                          | Demo and lab: tracing with OpenTelemetry and Application Insights                                                         |
| 4   | 10:20 to 10:30 | 10m    | Break                                                                                                        |                                                                                                                           |
| 4   | 10:30 to 11:40 | 1h 10m | [Agent Evaluators: Task Adherence & Tools](07-evaluation/02-agent-evaluators/readme.md)                      | Demo and lab: agent evaluators for task adherence and tool use                                                            |
| 4   | 11:40 to 12:40 | 1h 00m | Lunch                                                                                                        |                                                                                                                           |
| 4   | 12:40 to 13:50 | 1h 10m | [Continuous Evaluation & AI Red Teaming](07-evaluation/04-continuous-evaluation/readme.md)                   | Demo and lab: continuous evaluation and the AI Red Teaming Agent                                                          |
| 4   | 13:50 to 14:00 | 10m    | Break                                                                                                        |                                                                                                                           |
| 4   | 14:00 to 15:10 | 1h 10m | [Guardrails: Prompt Shields & Intervention Points](08-governance/01-guardrails/readme.md)                    | Demo and lab: guardrails with Prompt Shields and intervention points                                                      |
| 4   | 15:10 to 15:20 | 10m    | Break                                                                                                        |                                                                                                                           |
| 4   | 15:20 to 16:30 | 1h 10m | [Entra Agent ID: On-Behalf-Of Flows](08-governance/03-agent-identity/readme.md)                              | Demo and lab: Entra Agent ID with on-behalf-of flows                                                                      |
| 4   | 16:30 to 16:40 | 10m    | Feedback and Questions                                                                                       | Wrap-up, open questions and the extended catalog for follow-up deliveries                                                 |
