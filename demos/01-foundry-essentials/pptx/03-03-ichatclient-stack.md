---
slide: "03-03"
topic: 03-foundry-sdk
title: "The .NET packages behind IChatClient"
subtitle: "From application code down to the project endpoint"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/03-03-ichatclient-stack.svg
order: 10
source: demos/01-foundry-essentials/03-foundry-sdk/readme.md
gamma_prompt: >
  Slide titled "The .NET packages behind IChatClient", subtitle "From application code
  down to the project endpoint". Left half holds four short bullets: application code
  depends only on IChatClient from Microsoft.Extensions.AI; the AsIChatClient adapter
  turns the Foundry Responses client into an IChatClient; Azure.AI.Extensions.OpenAI
  2.0.0 provides the OpenAI-compatible clients and Azure.AI.Projects 2.0.1 the project
  client; the 10.4.1 adapter pair and Azure.AI.Extensions.OpenAI 2.0.0 both resolve the
  OpenAI package 2.9.1. Right half shows the supplied layer stack with package names and
  versions per layer. Clean and light, white background.
---

# The .NET packages behind IChatClient

## Content

- Application code depends only on **IChatClient** from Microsoft.Extensions.AI
- The **adapter** `AsIChatClient` turns the Foundry Responses client into an `IChatClient`
- **Azure.AI.Extensions.OpenAI** 2.0.0 provides the OpenAI-compatible clients, `Azure.AI.Projects` 2.0.1 the project client
- The **10.4.1** adapter pair and Azure.AI.Extensions.OpenAI 2.0.0 both resolve `OpenAI` 2.9.1

## Speaker Notes

In .NET, Microsoft.Extensions.AI adds a provider-neutral interface on top of the Foundry SDK. This slide stacks the packages from your code down to the project.

IChatClient: you write application code against IChatClient and decide at the composition root which model client sits behind it. The agent frameworks in later modules consume IChatClient as well, so this code carries over.

Adapter: Microsoft.Extensions.AI.OpenAI provides AsIChatClient, which turns the Foundry Responses client into an IChatClient. Pass the model to AsIChatClient(model) as well, or the service answers with HTTP 400.

Foundry packages: Azure.AI.Projects 2.0.x brings Azure.AI.Extensions.OpenAI, the generally available package with the OpenAI-compatible clients. Keep the preview Azure.AI.Projects.OpenAI out of the project, since both define the same types.

Pinned versions: Azure.AI.Extensions.OpenAI 2.0.0 is built against OpenAI 2.9.1, and Microsoft.Extensions.AI.OpenAI 10.4.1 depends on that same version. When you move to a newer Foundry package, move the adapter with it.
