---
slide: "00-02"
topic: 01-foundry-essentials
title: "Overview"
subtitle: "From one project endpoint to the context of every call"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/00-02-overview.svg
order: 2
source: demos/01-foundry-essentials/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "From one project endpoint to the context of every
  call". Layout: two columns. Left column: five short bullets walking the module path
  from the Foundry project and model router through the Foundry SDK and the Responses API
  to the operating tools and context engineering. Right column, wider than the left: the
  provided diagram of six numbered topic cards in two rows as an inline content image at
  its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a
  background or side accent. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Overview

## Content

- Start with one **Foundry project** and its endpoint, then let **model router** pick the model per call
- Reach both from code with the **Foundry SDK**, and in .NET through `IChatClient`
- Carry a conversation with the **Responses API** and stream the answer
- Operate the project from VS Code, `azd ai` and the **Foundry MCP server**
- Decide what goes into every call with **context engineering**: measure, trim, compact and cache

## Speaker Notes

This slide walks the six topics once, in the order you meet them when you build on Foundry. The top row gets you from a project to a working model call, the bottom row covers what you do with those calls.

Project and router: a Foundry resource holds the deployments, and its project gives your code one endpoint. Model router is deployed like any model and forwards each prompt to a suitable model from its pool.

SDK: the project client handles Foundry-native operations and hands you the OpenAI-compatible client for model calls. In .NET, `IChatClient` keeps the rest of the application independent of the vendor client.

Responses API: you choose whether the history lives in a stored response, a conversation or your own code. Streaming then sends the answer as events, so the first token arrives long before the last.

Tooling: the Foundry Toolkit, the `azd ai` extensions and the hosted Foundry MCP server replace slow portal work with an editor, a terminal or a chat.

Context engineering: every request carries its whole context, and you pay for it on every call. The last topic measures the budget, trims by turn, compacts and caches the stable prefix.
