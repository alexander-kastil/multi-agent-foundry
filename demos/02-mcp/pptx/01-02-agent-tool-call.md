---
slide: "01-02"
topic: 01-mcp-servers
title: "How an agent calls an MCP tool"
subtitle: "agent.py, a Foundry model and the HR server in eight messages"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/01-02-agent-tool-call.svg
order: 4
source: demos/02-mcp/01-mcp-servers/demo-mcp-servers.md
gamma_prompt: >
  Slide titled "How an agent calls an MCP tool", subtitle "agent.py, a Foundry model and the HR
  server in eight messages". Left half holds four short bullets: discovery, choice, execution and
  proof. Right half shows a sequence diagram with three lifelines, a purple Foundry model on the
  left, a blue agent.py in the middle and an amber HR MCP server on the right, and eight numbered
  messages from tools/list to the final answer, which sits in a green box at the bottom. White
  background, clean sans-serif font.
---

# How an agent calls an MCP tool

## Content

- **Discovery**: `MCPStreamableHTTPTool` lists the tools and their schemas
- **Choice**: the model reads the schemas and asks for `search_employees`
- **Execution**: the agent runs the `tools/call` and hands the result back
- **Proof**: the answer names Marie Dubois, whose data exists only in the server

## Speaker Notes

This slide follows the last step of the lab, where Agent Framework connects a Foundry model to the Python HR server. The agent sits in the middle and is the only part that talks to both sides.

Discovery: MCPStreamableHTTPTool connects to the server at 127.0.0.1:47003/mcp and lists its tools. The names, descriptions and JSON Schemas are all the model learns about the server.

Choice: the agent passes the question and the tool schemas to the gpt-5.4 deployment. The model picks search_employees to answer "Who on the team has baking skills, and which languages do they speak?"

Execution: the agent runs the call the model requested against the server. The demo sets approval_mode to never_require, so no confirmation stops the call.

Proof: the answer names Marie Dubois and her three languages, French, English and Italian. That proves the model called a tool, because the data exists only in the server.
