---
slide: "05-01"
topic: 05-tools-approvals
title: "Where tools run"
subtitle: "Your process calls function and local MCP tools, Foundry runs hosted tools"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/05-01-where-tools-run.svg
order: 12
source: demos/04-agent-framework/05-tools-approvals/readme.md
gamma_prompt: >
  Slide titled "Where tools run", subtitle "Your process calls function and local MCP tools,
  Foundry runs hosted tools". Left half holds five short bullets about function tools, local
  MCP tools and hosted tools in Microsoft Agent Framework. Right half shows an architecture
  map with a light blue area for your process (the agent, function tools, an MCP tool over
  stdio and an MCP tool over HTTP) and a light purple area for the Foundry model service
  (model deployment, code interpreter, hosted MCP), with a local child-process MCP server and
  the Microsoft Learn MCP server below, reached once from your process and once from Foundry.
  Clean minimal style, white background.
---

# Where tools run

## Content

- **Function tools** made with `@tool` run in your process; the model sees only name, description and result
- `MCPStdioTool` starts a local MCP server as a child process, `MCPStreamableHTTPTool` connects to a remote one from your process
- **Hosted tools** such as the code interpreter and hosted MCP run on the model service
- Trace context reaches only the MCP connections your process opens
- Every tool with side effects needs an approval, wherever it runs

## Speaker Notes

This slide puts every tool from the demo on one map: what runs inside your process and what the Foundry model service runs for you.

Function tools: `get_trail_status`, `estimate_price` and `book_hut` run in your process, and the model only sees their name, description and result. Use them for logic you own and want to test.

Local MCP tools: `MCPStdioTool` starts `trail_mcp_server.py` as a child process, and `MCPStreamableHTTPTool` connects your process to the Microsoft Learn MCP server. Your process stays in control, so you see every tool call.

Hosted tools: the code interpreter runs Python in a Foundry sandbox, and `get_mcp_tool` lets Foundry itself call the Learn server. The demo prints no tool call lines here, because the calls happen on the service.

Tracing and approval: trace context propagation to MCP servers works only for connections your process opens. Put every tool with side effects behind an approval, here `book_hut` and `reserve_bed`.
