---
slide: "00-01"
topic: 02-mcp
title: "MCP servers for Foundry agents"
subtitle: "From the first line of code to a hosted, secured and shared endpoint"
layout: title-only
visual-weight: 1/2
visual-type: photo
media-source: nano-banana
media-file: demos/02-mcp/pptx/images/00-01-mcp-for-agents.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no collage. Two software engineers, a woman and a man around thirty, modern work-casual, stand at a light-oak standing desk in a bright modern engineering studio. She types on a laptop while he points at a large wall display that shows softly blurred connected blocks, one block in the middle linked to several smaller ones; both are focused and relaxed. The laptop screen, the wall display and the coffee cups show no readable text, badges or logos. Modern, light, bright and friendly: high-key natural daylight, white and light-oak contemporary interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes and colours, never readable UI. 2:3 portrait, people centred in the frame, contemporary editorial photography. Avoid: readable text, logos, watermarks, extra people, dark or moody lighting, vintage or retro decor, grime, clutter.
order: 1
source: demos/02-mcp/readme.md
gamma_prompt: >
  Title slide "MCP servers for Foundry agents" with subtitle "From the first line of code to a hosted,
  secured and shared endpoint" and three short bullets about building and debugging servers,
  protecting and hosting them, and hardening and publishing them. Text on the left half on plain white,
  the provided photograph of two engineers at a standing desk in front of a wall display as a side
  accent on the right half, no text over it. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# MCP servers for Foundry agents

## Content

- **Build and debug** servers in C# and Python with the MCP Inspector
- **Protect** them with Microsoft Entra ID and **host** them on Azure Functions
- **Harden** them against backend failures and **publish** them once through a Foundry toolbox

## Speaker Notes

This module takes a tool server from the first line of code to a hosted, secured and shared endpoint that Microsoft Foundry agents consume. Every demo is a finished, run-verified project in its topic folder.

Build and debug: the servers are written in C# on `ModelContextProtocol` 2.2.0 and in Python on `mcp` 2.3.0. The MCP Inspector shows what they send and receive.

Protect and host: Microsoft Entra ID protects the servers, and Azure Functions hosts them at an endpoint a Foundry agent can reach.

Harden and publish: retry, a circuit breaker and a fallback keep a tool honest during an outage. A Foundry toolbox then publishes the tools once for every agent.
