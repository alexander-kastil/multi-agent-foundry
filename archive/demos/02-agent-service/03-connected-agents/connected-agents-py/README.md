# Connected Agents Python Demo

This demo demonstrates a multi-agent ticket triage system where a main orchestrator agent delegates specialized tasks to three connected sub-agents. The Priority Agent assesses ticket urgency, the Team Agent determines ownership, and the Effort Agent estimates required work. Each connected agent operates independently with its own instructions, while the main agent coordinates their outputs to provide comprehensive ticket analysis.

## What This Demo Shows

This example demonstrates the **ConnectedAgentTool** pattern in Azure AI Agent Service. Each specialized agent is wrapped as a `ConnectedAgentTool` and added to the main orchestrator agent's toolset. When the orchestrator receives a user query, it automatically delegates to the appropriate connected agents based on their descriptions, enabling multi-agent collaboration without manual routing logic. The demo showcases agent creation, tool registration, thread management, and proper cleanup of all agent resources.

## How to Run

1. Copy `.env.copy` to `.env` and configure your Azure AI project endpoint and model deployment
2. Install dependencies: `uv sync` (or `pip install -r requirements.txt`)
3. Run the agent: `uv run python agent_triage.py` (or `python agent_triage.py`)
4. Optional: Set `VERBOSE_OUTPUT=true` for detailed logging
5. Optional: Set `CREATE_MERMAID_DIAGRAM=true` to generate a Mermaid sequence diagram showing the agent interaction flow

## Diagram Generation

When `CREATE_MERMAID_DIAGRAM=true` is set, the script generates a single Markdown file containing:
- The ticket number as a heading
- The ticket description
- A **default diagram** showing the basic agent interaction flow
- A **verbose diagram** with detailed notes about each step, including the decision criteria for each agent (priority levels, team options, effort estimates)

The diagram file is saved as `TICKET-{timestamp}_triage_flow.md` and can be viewed in any Markdown viewer that supports Mermaid diagrams (like GitHub, VS Code, etc.).
