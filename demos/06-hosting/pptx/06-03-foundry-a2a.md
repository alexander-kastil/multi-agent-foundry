---
slide: "06-03"
topic: 06-a2a-protocol
title: "A2A in Foundry Agent Service"
subtitle: "Incoming calls on m6h2-a2a-agent, outgoing calls through the A2A tool"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/06-03-foundry-a2a.svg
order: 16
source: demos/06-hosting/06-a2a-protocol/demo-a2a-protocol.md
gamma_prompt: >
  Slide titled "A2A in Foundry Agent Service", subtitle "Incoming calls on m6h2-a2a-agent,
  outgoing calls through the A2A tool". Left half holds five short bullets: one PATCH adds
  an agent card and the a2a protocol to a prompt agent, a script calls it directly with an
  Entra token, a second prompt agent calls it through a RemoteA2A project connection with
  the A2A tool, that caller uses its own agent identity, and every calling identity needs
  the Foundry Agent Consumer role. Right half shows the supplied diagram of the two callers
  on the left, the connection in the middle and the target prompt agent on the right with
  its protocols, card and endpoint. Clean minimal style, white background, no extra imagery.
---

# A2A in Foundry Agent Service

## Content

- One PATCH adds an **agent card** and the **a2a protocol** next to responses
- Step 4 calls the agent directly with an **Entra token** and protocol 1.0
- Step 5 calls it from `m6h2-a2a-caller` through a **RemoteA2A connection**
- The **A2A tool** calls with the caller's own agent identity
- Every calling identity needs the **Foundry Agent Consumer** role

## Speaker Notes

Foundry can host the A2A endpoint of a prompt agent, and a Foundry agent can call out over A2A. This slide shows both directions from Steps 4 and 5, meeting at the same agent.

One PATCH: `enable_incoming.py` creates `m6h2-a2a-agent` and patches it with `update_details`, adding an agent card with the `releases` skill and the a2a protocol beside responses. The portal cannot do this yet, so it goes through the REST API or the SDK.

Entra token: `call_foundry_a2a.py` sends a bearer token for `https://ai.azure.com/.default`, reads the card from `agentCard/v1.0` and sends a v1.0 message. The answer is a task in state `TASK_STATE_COMPLETED` with the reply as an artifact.

RemoteA2A connection: `create_a2a_caller.py` creates the `m6h2-a2a-target` connection of category `RemoteA2A`, which points at the agent's A2A base path, and a caller agent that carries the `A2ATool`. The A2A tool replaces the Connected Agents tool of the classic Agents API.

Own identity: the caller reaches the target with its own agent identity, and `create_a2a_caller.py` prints its principal id. The response lists the delegation as two call items before the final message.

Consumer role: callers authenticate with Microsoft Entra only, and the calling identity needs the Foundry Agent Consumer role on the project or on the single agent. In the demo you assign that role to the caller identity on `m6h2-a2a-agent`.
