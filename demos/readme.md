# Implementing Multi-Agent Solutions using Microsoft Foundry & Microsoft Agent Framework

This four-day class is for software architects and engineers who build, orchestrate and operate agentic applications with Microsoft Foundry and the Microsoft Agent Framework. Every topic is taught with a runnable demo in Python and C#.

The class starts with the Foundry essentials and the MCP servers the agents call, covers server-side agents in the Foundry Agent Service and code-first agents in the Agent Framework, composes them into workflows and multi-agent orchestrations, hosts them, and makes them measurable and safe with evaluation, observability, guardrails and governance.

## Duration

4 Days

## Audience

- Software Architects & Engineers for Agentic AI Solutions
- AI Pro-Code Developers

## Prerequisites & Requirements

- Python, C#, Typescript
- GitHub Account
- Azure Subscription with access to Microsoft Foundry
- Azure Development Skills helpful

## Modules

## Module 1: Foundry Essentials for Agent Developers

- [Microsoft Foundry: Projects & Resources](./01-foundry-essentials/01-foundry-projects/)
- [Model Deployments & Model Router](./01-foundry-essentials/02-model-router/)
- [Foundry SDK & Microsoft.Extensions.AI](./01-foundry-essentials/03-foundry-sdk/)
- [Responses API: Conversations & Streaming](./01-foundry-essentials/04-responses-api/)
- [Foundry Toolkit & Foundry MCP Server](./01-foundry-essentials/05-foundry-toolkit/)
- [Context Engineering: Budgets, Compaction & Caching](./01-foundry-essentials/06-context-engineering/)

## Module 2: MCP Servers, Security & Hosting

- [MCP Servers: Tools, Resources & Prompts](./02-mcp/01-mcp-servers/)
- [Transports & MCP Inspector Debugging](./02-mcp/02-transports/)
- [MCP Authorization: OAuth 2.1 & PRM](./02-mcp/03-mcp-authorization/)
- [Hosting MCP on Azure Functions](./02-mcp/04-azure-functions/)
- [MCP Resilience: Retry & Circuit Breaker](./02-mcp/05-mcp-resilience/)
- [Foundry Toolbox: Managed MCP Endpoints](./02-mcp/06-foundry-toolbox/)

## Module 3: Foundry Agent Service: Tools, Knowledge & Memory

- [Prompt Agents: Versions & Conversations](./03-agent-service/01-prompt-agents/)
- [Code Interpreter, File & Web Search](./03-agent-service/02-builtin-tools/)
- [Browser Automation & Computer Use](./03-agent-service/03-browser-automation/)
- [Foundry IQ: Knowledge Bases & Retrieval](./03-agent-service/04-foundry-iq/)
- [Advanced RAG: Hybrid Search & Reranking](./03-agent-service/05-advanced-rag/)
- [Agent Memory: User & Procedural Memory](./03-agent-service/06-agent-memory/)
- [Stateful Agentic Loops: Plan & Resume](./03-agent-service/07-agentic-loops/)
- [Voice Agents: Voice Live API](./03-agent-service/08-voice-agents/)

## Module 4: Agent Framework: Agents, Tools & Skills

- [Chat Clients: Foundry, OpenAI & Claude](./04-agent-framework/01-chat-clients/)
- [AgentSession: State & Persistence](./04-agent-framework/02-agent-session/)
- [Context Providers & Long-Term Memory](./04-agent-framework/03-context-providers/)
- [Middleware: Agent, Function & Chat](./04-agent-framework/04-middleware/)
- [Function Tools, MCP & Approvals](./04-agent-framework/05-tools-approvals/)
- [Agent Skills: SKILL.md & Progressive Disclosure](./04-agent-framework/06-agent-skills/)
- [Harness Agent: Plan, Execute & Compaction](./04-agent-framework/07-harness-agent/)

## Module 5: Workflows & Multi-Agent Orchestration

- [Workflows: Executors, Edges & Supersteps](./05-orchestration/01-workflows/)
- [Checkpointing & Human-in-the-Loop Requests](./05-orchestration/02-checkpoints-hitl/)
- [Sequential, Concurrent & Handoff Orchestration](./05-orchestration/03-orchestration-patterns/)
- [Group Chat & Magentic Orchestration](./05-orchestration/04-magentic/)
- [Task Decomposition: Planner & Waves](./05-orchestration/05-task-decomposition/)
- [Resilient Orchestration: Quorum & Supervisors](./05-orchestration/06-resilient-orchestration/)
- [Declarative YAML & Functional Workflows](./05-orchestration/07-declarative-workflows/)

## Module 6: Hosting, Durable Agents & Protocols

- [Hosted Agents: Containers & Agent Identity](./06-hosting/01-hosted-agents/)
- [Foundry Routines: Schedules & Event Triggers](./06-hosting/02-routines/)
- [azd AI Agent: Deploy & Monitor](./06-hosting/03-azd-ai-agent/)
- [Durable Agents: Functions & Task Scheduler](./06-hosting/04-durable-agents/)
- [Self-Hosting: ASP.NET Core & Responses](./06-hosting/05-self-hosting/)
- [A2A Protocol: Agent Cards & Endpoints](./06-hosting/06-a2a-protocol/) (optional)
- [Enterprise A2A: Registry & Shared State](./06-hosting/07-enterprise-a2a/) (optional)
- [AG-UI & CopilotKit: Web Front Ends](./06-hosting/08-ag-ui/) (optional)

## Module 7: Evaluation, Observability & Optimization

- [Tracing: OpenTelemetry & Application Insights](./07-evaluation/01-tracing/)
- [Agent Evaluators: Task Adherence & Tools](./07-evaluation/02-agent-evaluators/)
- [Evaluation Suites: Judges & Baselines](./07-evaluation/03-evaluation-suites/)
- [Continuous Evaluation & AI Red Teaming](./07-evaluation/04-continuous-evaluation/)
- [Agent Optimizer: Instructions & Tool Tuning](./07-evaluation/05-agent-optimizer/)
- [Performance & Cost: Routing & Caching](./07-evaluation/06-performance-cost/)
- [Incident Response: Replay & Postmortem](./07-evaluation/07-incident-response/)

## Module 8: Security, Governance & Delivery

- [Guardrails: Prompt Shields & Intervention Points](./08-governance/01-guardrails/)
- [Prompt Injection: Layered Defenses](./08-governance/02-prompt-injection/)
- [Entra Agent ID: On-Behalf-Of Flows](./08-governance/03-agent-identity/)
- [Zero Trust: Tenant Isolation & Secrets](./08-governance/04-zero-trust/)
- [CI/CD: GitHub Actions Quality Gates](./08-governance/05-cicd-gates/)
- [Agent Lifecycle: Versions, Quotas & Retirement](./08-governance/06-agent-lifecycle/)
- [Responsible AI: Fairness Probes & Audit](./08-governance/07-responsible-ai/)

## Schedule

| Day | Clock          | Length | Deliverable                                                                    | Description                                                                                                                                                                                                                                          |
| --- | -------------- | ------ | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | 09:00 to 10:05 | 1h 05m | [Foundry Essentials for Agent Developers](01-foundry-essentials/readme.md)     | Microsoft Foundry, Model Deployments & Model Router, Foundry SDK & Microsoft.Extensions.AI, Responses API, Foundry Toolkit & Foundry MCP Server, Context Engineering (6 topics, 6 demos)                                                             |
| 1   | 10:05 to 10:15 | 10m    | Break                                                                          |                                                                                                                                                                                                                                                      |
| 1   | 10:15 to 11:30 | 1h 15m | [MCP Servers, Security & Hosting](02-mcp/readme.md)                            | MCP Servers, Transports & MCP Inspector Debugging, MCP Authorization, Hosting MCP on Azure Functions, MCP Resilience, Foundry Toolbox (6 topics, 6 demos)                                                                                            |
| 1   | 11:30 to 12:30 | 1h 00m | Lunch                                                                          |                                                                                                                                                                                                                                                      |
| 1   | 12:30 to 13:55 | 1h 25m | [Foundry Agent Service: Tools, Knowledge & Memory](03-agent-service/readme.md) | Prompt Agents, Code Interpreter, File & Web Search, Browser Automation & Computer Use, Foundry IQ, Advanced RAG, Agent Memory, Stateful Agentic Loops, Voice Agents (8 topics, 8 demos)                                                              |
| 1   | 13:55 to 14:55 | 1h 00m | [Agent Framework: Agents, Tools & Skills](04-agent-framework/readme.md)        | Chat Clients, AgentSession, Context Providers & Long-Term Memory, Middleware, Function Tools, MCP & Approvals, Agent Skills, Harness Agent (7 topics, 7 demos)                                                                                       |
| 1   | 14:55 to 15:05 | 10m    | Break                                                                          |                                                                                                                                                                                                                                                      |
| 1   | 15:05 to 16:10 | 1h 05m | [Workflows & Multi-Agent Orchestration](05-orchestration/readme.md)            | Workflows, Checkpointing & Human-in-the-Loop Requests, Sequential, Concurrent & Handoff Orchestration, Group Chat & Magentic Orchestration, Task Decomposition, Resilient Orchestration, Declarative YAML & Functional Workflows (7 topics, 7 demos) |
| 2   | 09:00 to 09:50 | 50m    | [Hosting, Durable Agents & Protocols](06-hosting/readme.md)                    | Hosted Agents, Foundry Routines, azd AI Agent, Durable Agents, Self-Hosting, A2A Protocol, Enterprise A2A, AG-UI & CopilotKit (8 topics, 8 demos, 3 optional)                                                                                        |
| 2   | 09:50 to 11:05 | 1h 15m | [Evaluation, Observability & Optimization](07-evaluation/readme.md)            | Tracing, Agent Evaluators, Evaluation Suites, Continuous Evaluation & AI Red Teaming, Agent Optimizer, Performance & Cost, Incident Response (7 topics, 7 demos)                                                                                     |
| 2   | 11:05 to 11:15 | 10m    | Break                                                                          |                                                                                                                                                                                                                                                      |
| 2   | 11:15 to 12:30 | 1h 15m | [Security, Governance & Delivery](08-governance/readme.md)                     | Guardrails, Prompt Injection, Entra Agent ID, Zero Trust, CI/CD, Agent Lifecycle, Responsible AI (7 topics, 7 demos)                                                                                                                                 |
