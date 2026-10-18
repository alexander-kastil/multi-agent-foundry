---
slide: "04-02"
topic: 04-azure-functions
title: "Connecting a Foundry agent"
subtitle: "The function key travels in a project connection"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/04-02-foundry-agent-connection.svg
order: 10
source: demos/02-mcp/04-azure-functions/demo-azure-functions.md
gamma_prompt: >
  Slide titled "Connecting a Foundry agent" with subtitle "The function key travels in a project
  connection". Left half: four short bullets about the prompt agent, the project connection, Agent
  Service attaching the header and the system key check. Right half: the provided diagram of the
  request path from agent.py through Foundry Agent Service and the project connection to the function
  app and blob storage, at full legibility on white. Use exactly the one image given for this card; do
  not generate or add any other images, icons or logos.
---

# Connecting a Foundry agent

## Content

- `agent.py` creates a **prompt agent** whose `MCPTool` names the connection
- The **project connection** `m2-func-mcp-key` holds the key as `x-functions-key`
- **Agent Service** attaches the header on every call to `/runtime/webhooks/mcp`
- The host checks the **system key** `mcp_extension`; without it the answer is `401`

## Speaker Notes

This slide follows one request from the agent script to blob storage after the deployment to Flex Consumption. A Foundry agent cannot reach `localhost`, so the hosted endpoint is the only way in.

The agent: `agent.py` creates the prompt agent `m2-functions-mcp-agent`, and its `MCPTool` points at the function app and names the connection. The key never appears in the instructions or the code.

The connection: `create-connection.azcli` writes a `RemoteTool` connection with the custom key `x-functions-key`. Every user with access to the project can read that key, so prefer Microsoft Entra authentication for production.

Agent Service: it attaches the header from the connection on every call to the MCP endpoint. The agent output shows one `mcp_call` for `save_snippet` and one for `get_snippet`.

The system key: the host requires `mcp_extension` in the `x-functions-key` header or the `code` query parameter. A request without it gets `401`, the first check in the lab.
