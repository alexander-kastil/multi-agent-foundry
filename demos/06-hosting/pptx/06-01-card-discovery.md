---
slide: "06-01"
topic: 06-a2a-protocol
title: "Agent card discovery"
subtitle: "Fetch the card, then send work to the interface it names"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/06-01-card-discovery.svg
order: 14
source: demos/06-hosting/06-a2a-protocol/demo-a2a-protocol.md
gamma_prompt: >
  Slide titled "Agent card discovery", subtitle "Fetch the card, then send work to the
  interface it names". Left half holds five short bullets: an A2A server publishes its
  agent card at a well-known path, the card lists name, skills and the interface URL, a
  client sends a message to that URL over JSON-RPC, the reply is a message or a task, and
  the remote instructions, tools and model stay on the server. Right half shows the
  supplied diagram of a Python client and an A2A server with four numbered calls: fetch
  the card, receive the card, send a message, receive the reply. Clean minimal style, white
  background, no extra imagery.
---

# Agent card discovery

## Content

- The server publishes its **agent card** at `/.well-known/agent-card.json`
- The card lists the name, the **skills** and the interface URL with its binding
- `A2AAgent` sends a **message** to that URL over JSON-RPC
- The reply is a **message or a task** with state and artifacts
- Instructions, tools and model **stay on the server**

## Speaker Notes

Every A2A call starts with discovery. This slide follows `client.py` from Step 2 as it reads the title agent's card and then sends it work.

Agent card: the self-hosted server publishes its card at `/.well-known/agent-card.json`, and `A2ACardResolver` fetches it. The card is the contract between the two sides.

Card contents: the card names `m6h2-title-agent`, its `generate_title` skill and one interface, `http://localhost:9101/` with the JSON-RPC binding. A2A also defines HTTP+JSON and gRPC bindings, and a server can offer several at once.

Message: `A2AAgent` wraps the remote endpoint as an ordinary Agent Framework agent, so the client calls it like a local one. On the server, `A2AExecutor` adapts the Agent Framework agent to the `a2a-sdk` server.

Reply: the server answers with a direct message or with a task that has a state and artifacts. Here the reply carries the title "Hosted Agents Without the Plumbing: Build Smarter, Ship Faster".

Stays on the server: the caller sees only the card and the answers. The remote instructions, tools and model never leave the server.
