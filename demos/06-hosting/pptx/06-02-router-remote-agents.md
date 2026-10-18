---
slide: "06-02"
topic: 06-a2a-protocol
title: "Routing between remote A2A agents"
subtitle: "Each A2AAgent becomes a tool for the router"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/06-02-router-remote-agents.svg
order: 15
source: demos/06-hosting/06-a2a-protocol/demo-a2a-protocol.md
gamma_prompt: >
  Slide titled "Routing between remote A2A agents", subtitle "Each A2AAgent becomes a tool
  for the router". Left half holds five short bullets: the client reads both agent cards
  first, as_tool turns each remote agent into a function tool, the router asks for a title
  first and then passes it to the outline agent, one answer carries both, and a third
  specialist is one more URL in a list. Right half shows the supplied diagram of a router
  agent with two tools calling a title agent and an outline agent over A2A, in numbered
  order. Clean minimal style, white background, no extra imagery.
---

# Routing between remote A2A agents

## Content

- `A2ACardResolver` reads both **agent cards** before routing
- `as_tool()` turns each `A2AAgent` into a **function tool**
- The router asks for a **title first**, then passes it to the outline agent
- One answer carries **both** the title and the outline
- A third specialist is **one more URL** in `REMOTES`

## Speaker Notes

Because `A2AAgent` is an agent, it can also be a tool. Step 3 builds a router that delegates to two remote specialists without knowing how either of them is built.

Cards first: `routing_client.py` resolves the card of the title agent on port 9101 and of the outline agent on port 9102. The run prints one found remote agent line for each.

Function tools: `as_tool()` turns each `A2AAgent` into a tool whose name and description come from its card. The router knows nothing else about the remote agents.

Title first: the router's instructions say to ask the title agent for a title, then pass that title to the outline agent. Both calls go over A2A to the two servers started in Step 1.

One answer: the router returns the title and the outline together. In the Verify section, both remote agents appear in the found lines and one answer carries both.

One more URL: adding a third specialist means adding its URL to the `REMOTES` list. The client resolves its card like the other two.
