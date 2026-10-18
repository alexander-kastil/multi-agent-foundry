# Pro-Code Agentic AI with Microsoft Foundry, the Agent Framework & Copilot Cowork

Embark on a five-day pro-code masterclass in Agentic AI engineering with Microsoft Foundry, the Microsoft Agent Framework, and Microsoft 365 Copilot Cowork. Built for Software Architects and Senior Engineers with the AI-103 foundation, it skips the hello-agent tutorial and goes straight to the architecture, identity, orchestration, and integration patterns that ship.

You start by reframing Microsoft Foundry as an architect sees it: the new resource model, the Azure OpenAI upgrade path, and Model Router across Balanced, Cost, and Quality bands. You wire the Foundry SDK 2.0 through Microsoft.Extensions.AI pipelines, replace threads and runs with the Responses API trio of agent, conversation, and response, and treat Agent Identity as a first-class concern with Entra service principals, Blueprints, and On-Behalf-Of flows. MCP becomes production infrastructure via the Azure Functions extension and Foundry Toolbox, with OpenTelemetry GenAI conventions powering trace-level evaluation, Prompt Optimizer, and Task Adherence.

From there you move into the Foundry Agent Service, navigating the three agent kinds (prompt, workflow, hosted) and the new state model. Foundry IQ opens up: query planner, ACL sync with Microsoft Purview, and the distinction between Foundry IQ, Fabric IQ, and Work IQ. Tools span Toolbox, OpenAPI, Functions MCP, o3-deep-research, Browser Automation, Computer Use, and Voice Live, before Hosted Agents with container deployment, Responses, Invocations, Activity, and A2A protocols, and the Connected Agents replacement.

You then go deep on the Microsoft Agent Framework. The three layers (Chat Clients, Agents, Workflows) separate concerns deliberately, and AgentSession persistence unlocks portable, durable conversations. Agent Skills land with SKILL.md, SkillsProvider, and progressive disclosure; Second Brain patterns extend them into episodic, semantic, and procedural memory via custom AIContextProviders. Orchestration spans Sequential, Concurrent, Handoff, Group Chat, and Magentic with its task ledger, stall detection, and plan-review HITL. Durable Agents on Azure Durable Functions and Flex Consumption deliver scale-to-zero workflows with checkpointing at superstep boundaries.

Finally, you ship the work into Microsoft 365 Copilot Cowork. You map the integration topologies (direct skill, Foundry behind MCP, Toolbox only, connector as team) and pick where reasoning lives. The headline pattern wraps a Foundry agent as an MCP server through the Azure Functions MCP extension, with identity flowing end to end via Entra SSO, the Microsoft Enterprise Token Store, and On-Behalf-Of into Foundry. You assemble Cowork plugins with agentConnectors, agentSkills, and SKILL.md coordinator skills, then sideload and distribute through Admin Center with Microsoft Purview audit.

By the end you will ship agentic teams that are production-grade by design: identity-aware, observable, durable, governable, and reachable from where business users actually work.

## Duration

5 Days

## Audience

- Software Architects & Engineers for Agentic AI Solutions
- Microsoft 365 & AI Pro-Code Developers

## Prerequisites & Requirements

- AI-103 or equivalent foundation in Microsoft Foundry & Agents
- Python, C#, Typescript
- GitHub Account
- Microsoft 365 Development & Azure Development Skills helpful

## Modules

## Module 1: Microsoft Foundry Pro Architecture & MCP

### Foundry Platform & SDK Essentials

- New Foundry Resource Model vs Hub-Based Projects
- Azure OpenAI to Foundry Upgrade Path
- Model Catalog: Azure Direct vs Partners & Community
- Model Router Strategy: Balanced, Cost & Quality Modes
- Foundry SDK 2.0 Package Matrix & Duplicate-Types Pitfall
- Microsoft.Extensions.AI Pipeline Composition
- Infrastructure as Code for Foundry Agents using Azure Developer CLI

### Responses API & Agent Identity

- Responses API as Canonical Surface (Agent, Conversation & Response)
- Conversation Continuity: previous_response_id vs Conversation Id
- Streaming, Background Mode & Response Replay
- Agent Identity Platform & Entra Service Principals
- Agent Identity Blueprints for Class-Scoped Governance
- On-Behalf-Of, Client Credentials & Managed Identity Patterns

### MCP Production Patterns & Trace-Level Observability

- MCP Tool Design: Single-Tool, Multi-Tool & Hybrid
- Hosting MCP in Azure Functions (MCP Extension, Flex Consumption)
- Foundry Toolbox as a Versioned MCP Endpoint
- OpenTelemetry GenAI Semantic Conventions
- Auto-Instrumentation Across Agent Frameworks
- Trace Evaluation Against Production Application Insights Spans
- Evaluator Catalogue: Quality, RAG, Agent-Specific & Custom
- Cluster Analysis, Prompt Optimizer & Task Adherence

## Module 2: Foundry Agent Service — Production Agents

- Three Agent Kinds: Prompt, Workflow & Hosted Agents
- Conversations, Responses & State Persistence (replacing Threads & Runs)
- Foundry IQ Deep: Query Planner, Reasoning Effort & ACL Sync
- The Three IQs: Foundry IQ, Fabric IQ & Work IQ
- Agent Memory vs Foundry IQ vs File Search
- Tool Ecosystem: Toolbox, Code Interpreter, OpenAPI & Functions MCP
- Deep Research Pattern using o3-deep-research with web_search and MCP
- Browser Automation in Microsoft Playwright Workspace & Computer Use
- Voice Live API: WebSocket Realtime with Foundry Agent Binding
- Hosted Agents: Container Deployment, Protocols & Agent Identity
- Hosted Agent Protocols: Responses, Invocations, Activity & A2A
- A2A Tool vs Workflow Handoff (Replacing Connected Agents)
- Production Tracing, Trace-Level Evaluation & Monitoring

## Module 3: Orchestrate Agents using Microsoft Agent Framework

### Agent Framework Architecture & Sessions

- Chat Clients vs Agents vs Workflows: Three Layers, Three Purposes
- Agent Pipeline: Middleware, Context Providers & Chat Client
- AgentSession Persistence: In-Memory, Cosmos & Server-Side
- Session Serialization & Cross-Provider Portability
- Hosting Agents in ASP.NET Core & Python (AddAIAgent, Responses API)
- A2A & AG-UI Protocol Adapters
- Cross-Stack: Deploying Agent Framework Agents as Foundry Hosted Agents

### Agent Skills, Tools & Middleware

- Agent Skills: SKILL.md, SkillsProvider & Progressive Disclosure
- Code-Defined vs File-Based vs Class-Based Skills
- Agent Skills vs Workflows: When to Use Each
- Second Brain Patterns: Episodic, Semantic & Procedural Memory
- Long-Term Memory via Custom AIContextProvider, Vector Stores & Persistent Knowledge Bases
- Tools Across Providers: Function, Local MCP, Hosted MCP & OpenAPI
- Agent-as-Tool vs Agent-as-MCP-Server
- Function-Calling, Agent & Chat-Client Middleware Layers
- OpenTelemetry GenAI Semantic Conventions for Agents

### Orchestration, Magentic & Durable Agents

- Workflows: WorkflowBuilder, Typed Edges & Pregel Supersteps
- Orchestration Patterns: Sequential, Concurrent, Handoff & Group Chat
- Magentic Orchestration: Task Ledger, Stall Detection & Replan
- Plan-Review Human-in-the-Loop with Magentic
- Handoff vs Agent-as-Tool: Ownership Semantics
- Durable Agents with Azure Durable Functions (Flex Consumption, Durable Task Scheduler)
- Checkpointing at Superstep Boundaries (File, Cosmos & Custom Storage)
- Workflow HITL: RequestPort & Approval-Required Tools
- Observability & Workflow Visualization

## Module 4: Use Foundry Agents in Microsoft 365 Copilot Cowork

- Copilot Cowork as a Target Surface for Foundry Agents (vs Chat & Copilot Studio)
- Integration Topologies: Direct Skill, Foundry-Behind-MCP, Toolbox-Only & Connector-as-Team
- Wrapping a Foundry Agent as an MCP Server with the Azure Functions MCP Extension
- Single-Tool vs Multi-Tool vs Hybrid Wrapper Design
- Identity End-to-End: Entra SSO, Microsoft Enterprise Token Store & On-Behalf-Of Flow
- Preserving Agent Identity Audit through OBO to Foundry
- Cowork Plugin Anatomy: agentConnectors, agentSkills & devPreview Manifest
- Skills as Orchestration Glue: SKILL.md, Progressive Disclosure & Coordinator Skills
- Building Agentic Teams of Foundry Agents Behind One Connector
- Wrapping Agent Framework Agents (AsAIFunction, McpServerTool, as_mcp_server)
- Packaging, Sideloading & Tenant Distribution via Admin Center
