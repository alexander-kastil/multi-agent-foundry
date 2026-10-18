# Module 3: Foundry Agent Service: Tools, Knowledge & Memory

Foundry Agent Service hosts agents for you: you publish a versioned definition of a model, instructions and tools, and the service runs it. This module takes a prompt agent from its first version to an agent that runs code, reads documents, browses the web, retrieves from enterprise knowledge, remembers its users, plans its own work and speaks.

The stack is the `azure-ai-projects` 2.x Python SDK and the `Azure.AI.Projects` 2.0.x .NET packages, Azure AI Search for retrieval, and the Voice Live SDKs. Every topic has a runnable project and a demo guide with real captured output. Classic threads and runs are not taught here because agents (classic) retire on March 31, 2027.

Each project has its own `.env.example`, a pinned `requirements.txt` or `.csproj` and a `.gitignore`. Copy `.env.example` to `.env`, set your project endpoint and run. The demos that need Azure resources ship a `provision.azcli` and, where useful, a `deprovision.azcli` beside the guide.

| Topic | Description |
| --- | --- |
| [01 Prompt Agents: Versions & Conversations](01-prompt-agents/readme.md) | Agent versions with `create_version`, `name:version` references, conversations, items and responses; the move away from classic threads and runs |
| [02 Code Interpreter, File & Web Search](02-builtin-tools/readme.md) | The generally available built-in tools: sandboxed Python, vector-store search over your files and web grounding with citations |
| [03 Browser Automation & Computer Use](03-browser-automation/readme.md) | A managed Playwright browser for an agent, and the screenshot loop and access gate of Computer Use |
| [04 Foundry IQ: Knowledge Bases & Retrieval](04-foundry-iq/readme.md) | Knowledge bases on Azure AI Search with agentic retrieval, reasoning effort and an MCP connection to the agent |
| [05 Advanced RAG: Hybrid Search & Reranking](05-advanced-rag/readme.md) | Hybrid search with RRF, per-source weights, rerankers, source routing and chunking, measured by mean reciprocal rank |
| [06 Agent Memory: User & Procedural Memory](06-agent-memory/readme.md) | Memory stores with user profile, chat summary and procedural memory, scope partitioning and the memory search tool |
| [07 Stateful Agentic Loops: Plan & Resume](07-agentic-loops/readme.md) | Plan then act, reflection with a stopping rule, parallel forks and resuming a session from a stored conversation id |
| [08 Voice Agents: Voice Live API](08-voice-agents/readme.md) | Voice Live in front of a Foundry agent and voice-based prompt agents, driven without a microphone |

| Topic | Description |
| --- | --- |
| [Version and Converse with a Prompt Agent](01-prompt-agents/demo-prompt-agents.md) | Publish two versions, pin calls by version, keep a server-side conversation and continue it on the newer version (Python and C#) |
| [Build a Data Analyst, Product Expert and Web Scout](02-builtin-tools/demo-builtin-tools.md) | One agent per built-in tool, a downloaded chart, cited answers and a combined agent (Python and C#) |
| [Drive a Managed Browser from an Agent](03-browser-automation/demo-browser-automation.md) | Provision a Playwright workspace, run a three-task browsing session and probe the Computer Use gate (Python and C#) |
| [Build a Knowledge Base and Connect an Agent to It](04-foundry-iq/demo-foundry-iq.md) | Two knowledge sources, three reasoning efforts compared and a Python and a C# agent over MCP |
| [Tune Hybrid Search, Reranking and Routing](05-advanced-rag/demo-advanced-rag.md) | Measure keyword, vector and hybrid retrieval, add rerankers, route across sources and run the semantic ranker on a real index (Python and C#) |
| [Give an Agent a Memory and Teach It a Routine](06-agent-memory/demo-agent-memory.md) | Create a memory store, write and search scoped memories, attach the memory search tool and prove isolation (Python and C#) |
| [Plan, Reflect, Fork and Resume an Analyst Agent](07-agentic-loops/demo-agentic-loops.md) | Four loop designs around one tool-using agent, ending in a resume from a second process (Python and C#) |
| [Talk to Two Kinds of Voice Agent](08-voice-agents/demo-voice-agents.md) | A voice-based prompt agent and a Voice Live agent, typed in and spoken out, with a stored conversation read back (Python and C#) |

## Helpful Claude Slash Commands

| Command | Usage |
| --- | --- |
| `/plan` | Plan an agent design (tools, knowledge, memory) before writing code |
| `/mcp` | Manage the Microsoft Learn MCP server that you use to check current Foundry APIs |
| `/model` | Switch the model that writes and reviews agent code |
| `/context` | See how much of the context window pasted documentation uses |
| `/review` | Review the agent code on a branch before you merge it |
| `/security-review` | Audit agents with browser, memory or retrieval tools for prompt injection and data exposure |

## Key Topics covered in this module

- [Foundry Agent Service overview](https://learn.microsoft.com/azure/foundry/agents/overview)
- [Build with agents, conversations, and responses](https://learn.microsoft.com/azure/foundry/agents/concepts/runtime-components)
- [Migrate to the Agents SDK](https://learn.microsoft.com/azure/foundry/agents/how-to/migrate)
- [Code Interpreter tool](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/code-interpreter)
- [File search tool](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/file-search)
- [Web search tool](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/web-search)
- [Browser Automation tool](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/browser-automation)
- [Computer use tool (preview)](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/computer-use)
- [What is Foundry IQ?](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq)
- [Connect a Foundry IQ knowledge base to Foundry Agent Service](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect)
- [Hybrid search overview](https://learn.microsoft.com/azure/search/hybrid-search-overview)
- [Memory in Foundry Agent Service (preview)](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-memory)
- [Create and use memory in Foundry Agent Service (preview)](https://learn.microsoft.com/azure/foundry/agents/how-to/memory-usage)
- [Quickstart: Voice Live with Foundry Agent Service](https://learn.microsoft.com/azure/ai-services/speech-service/voice-live-agents-quickstart)
- [Quickstart: Create a voice-based prompt agent](https://learn.microsoft.com/azure/foundry/agents/quickstarts/prompt-voice-agent)
