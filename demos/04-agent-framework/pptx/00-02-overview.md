---
slide: "00-02"
topic: 04-agent-framework
title: "Overview"
subtitle: "The path through the module"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/00-02-overview.svg
order: 2
source: demos/04-agent-framework/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "The path through the module". Two columns. Left column: the bullets.
  Right column, wider than the left: the provided diagram of seven numbered topic cards in two rows joined by
  arrows, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never
  cropped, never used as a background or side accent. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Overview

## Content

- **Chat clients** decide where the model runs; the agent code stays the same
- **Sessions and context providers** carry history, preferences and memory between runs
- **Middleware** wraps every run, tool call and model request from the outside
- **Tools, MCP and approvals** let the agent act, with a human in front of what cannot be undone
- **Skills and the harness agent** load procedures on demand and run long tasks with plans, todos and compaction

## Speaker Notes

This slide walks the seven topics once, in the order the module builds the agent. Every topic pairs the teaching with a runnable Python and C# demo against a Microsoft Foundry project.

Chat clients: swapping the client changes where the model runs, and the agent code around it stays the same. The one decision is whether your code or Foundry owns the agent definition.

Sessions and context providers: AgentSession carries a conversation from one run to the next and is the unit you save. Context providers add history, preferences and long-term memory to each request.

Middleware: logging, guardrails and metering wrap the agent from the outside. One function can watch or change every run, every tool call or every model request.

Tools and approvals: function tools, MCP servers and hosted tools let the agent act. The approval gate keeps a human in control of actions that cannot be undone.

Skills and the harness agent: skills load a company procedure only when the task needs it. The harness agent bundles plans, todos, standing approvals, compaction and file memory for long tasks.
