---
slide: "00-02"
topic: 02-mcp
title: "Overview"
subtitle: "The path through the module"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/00-02-overview.svg
order: 2
source: demos/02-mcp/readme.md
gamma_prompt: >
  Slide titled "Overview" with subtitle "The path through the module". Left: four short bullets
  walking the path from building a server to publishing it in a toolbox. Right, wider: the provided
  diagram of six numbered topic cards in two rows. Use exactly the one image given for this card; do
  not generate or add any other images, icons or logos.
---

# Overview

## Content

- Build one HR server in **C# and Python** with tools, resources and prompts
- Compare **stdio** and **Streamable HTTP**, and debug with the MCP Inspector
- Protect the server with **Microsoft Entra ID** and host it on **Azure Functions**
- Harden it against backend outages, then publish it once in a **Foundry toolbox**

## Speaker Notes

This slide walks the path through the six topics once. The first row builds and protects a server, the second row hosts, hardens and shares it.

Build: topic 1 builds the same HR server in C# and Python, with tools, resources and prompts.

Debug: topic 2 runs one server over stdio and Streamable HTTP and reads the raw messages with the MCP Inspector.

Protect and host: topic 3 secures the server with Entra ID tokens, scopes and step-up, and topic 4 deploys it to Azure Functions with a key that a Foundry agent can use.

Harden and share: topic 5 keeps the server honest during a backend outage with retry, a circuit breaker and a fallback, and topic 6 publishes the tools once through a Foundry toolbox.
