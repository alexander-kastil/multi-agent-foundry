---
slide: "01-02"
topic: 01-chat-clients
title: "Who owns the agent definition"
subtitle: "FoundryChatClient keeps it in your code, FoundryAgent keeps it in Foundry"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/01-02-agent-definition-owner.svg
order: 4
source: demos/04-agent-framework/01-chat-clients/demo-chat-clients.md
gamma_prompt: >
  Slide titled "Who owns the agent definition", subtitle "FoundryChatClient keeps it
  in your code, FoundryAgent keeps it in Foundry". Left half holds four short bullets
  comparing an Agent built on FoundryChatClient, whose instructions, tools, middleware
  and session handling live in the developer's process, with FoundryAgent, which
  connects to a Prompt Agent stored in Microsoft Foundry that supplies the model,
  instructions and hosted tools. Right half shows the supplied two-column diagram of
  what sits in your process and what sits in Foundry for each option. Clean minimal
  style, white background, no extra imagery.
---

# Who owns the agent definition

## Content

- With **FoundryChatClient**, instructions, tools, middleware and sessions live in your process
- **FoundryAgent** connects to a Prompt Agent or Hosted Agent stored in Foundry
- The **stored definition** supplies the model, instructions and hosted tools
- Local `tools=` callables only implement functions the agent already declares

## Speaker Notes

Picking a client comes down to one question: does your code own the agent definition, or does Microsoft Foundry? The diagram puts the same items on each side of that line.

FoundryChatClient: instructions, tools, middleware and session handling all live in your process. Use `Agent(client=FoundryChatClient(...))` whenever you need per-run control over instructions or tools.

FoundryAgent: it connects to an agent that already exists in the Foundry Agent Service, either a named, versioned Prompt Agent or a deployed Hosted Agent. In the demo, `create_prompt_agent.py` registers `m4-support-agent` and Foundry assigns version 1.

Stored definition: the model, instructions and hosted tools come from Foundry, so the client cannot replace them at run time. The demo's answer quotes the 30 day return window that no script typed, and a per-run instructions override comes back with "Not allowed when agent is specified."

Local tools: a Python callable passed through `tools=` only supplies the implementation of a function the Foundry agent declares. It never adds a new tool to the definition.
