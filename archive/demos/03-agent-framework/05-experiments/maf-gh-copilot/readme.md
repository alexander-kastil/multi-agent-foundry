# GitHub Copilot SDK with Microsoft Agent Framework

This demo collection showcases how to build AI agents using GitHub Copilot SDK integrated with Microsoft Agent Framework in Python.

## Overview

The Microsoft Agent Framework integrates with the GitHub Copilot SDK, enabling you to build AI agents powered by GitHub Copilot with support for:

- Function calling and custom tools
- Streaming responses
- Multi-turn conversations
- Shell command execution, file operations, and URL fetching
- Model Context Protocol (MCP) server integration
- Multi-agent workflows with orchestration

## Prerequisites

- Python 3.11+
- GitHub Copilot subscription or access
- Azure CLI authentication (for Azure OpenAI agent example)

## Installation

1. Create a virtual environment:

   ```bash
   python -m venv .venv
   ```

2. Activate the virtual environment:
   - **Windows PowerShell**: `.venv\Scripts\Activate.ps1`
   - **Windows CMD**: `.venv\Scripts\activate.bat`
   - **Linux/macOS**: `source .venv/bin/activate`

3. Install dependencies (note: packages are pre-release, so use `--pre` flag):
   ```bash
   pip install -r requirements.txt --pre
   ```

## Demo Examples

### 1. Basic Agent (`01-basic-agent.py`)

Create a simple agent that answers questions using GitHub Copilot.

```bash
python 01-basic-agent.py
```

### 2. Function Tools (`02-function-tools.py`)

Extend the agent with custom function tools to give it domain-specific capabilities.

```bash
python 02-function-tools.py
```

### 3. Streaming Responses (`03-streaming-responses.py`)

Stream responses as they are generated for a better user experience.

```bash
python 03-streaming-responses.py
```

### 4. Multi-Turn Conversations (`04-multi-turn-conversations.py`)

Maintain conversation context across multiple interactions using threads.

```bash
python 04-multi-turn-conversations.py
```

### 5. Permissions (`05-permissions.py`)

Enable the agent to execute shell commands, read/write files, and fetch URLs with interactive permission prompts.

```bash
python 05-permissions.py
```

### 6. MCP Servers (`06-mcp-servers.py`)

Connect to local (stdio) and remote (HTTP) MCP servers to give the agent access to external tools and data sources.

```bash
python 06-mcp-servers.py
```

### 7. Multi-Agent Workflow (`07-multi-agent-workflow.py`)

Combine GitHub Copilot agents with other agents (like Azure OpenAI) in a sequential workflow using the `SequentialBuilder` orchestrator.

```bash
python 07-multi-agent-workflow.py
```

**Note:** This example requires Azure OpenAI credentials configured with `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` environment variables. The workflow demonstrates:

- Creating a copywriter agent (Azure OpenAI) that drafts marketing content
- Creating a reviewer agent (GitHub Copilot) that provides feedback
- Orchestrating agents in a sequential pipeline where output flows from one agent to the next

## Key Benefits

Using Agent Framework with GitHub Copilot SDK provides:

- **Consistent agent abstraction** — GitHub Copilot agents implement the same `BaseAgent` interface as every other agent type
- **Multi-agent workflows** — Compose GitHub Copilot agents with other agents in sequential, concurrent, handoff, and group chat workflows
- **Ecosystem integration** — Access the full Agent Framework ecosystem with declarative agents, A2A protocol support, and consistent patterns

## References

- [GitHub Copilot SDK](https://github.com/github/copilot-sdk)
- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
- [Agent Framework Getting Started Tutorials](https://learn.microsoft.com/agent-framework/tutorials/overview)
- [Blog Post: Build AI Agents with GitHub Copilot SDK and Microsoft Agent Framework](https://devblogs.microsoft.com/semantic-kernel/build-ai-agents-with-github-copilot-sdk-and-microsoft-agent-framework/)
