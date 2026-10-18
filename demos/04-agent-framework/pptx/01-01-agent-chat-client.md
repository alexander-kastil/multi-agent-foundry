---
slide: "01-01"
topic: 01-chat-clients
title: "Agent and chat client"
subtitle: "Swapping the client changes where the model runs"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/01-01-agent-chat-client.svg
order: 3
source: demos/04-agent-framework/01-chat-clients/readme.md
gamma_prompt: >
  Slide titled "Agent and chat client", subtitle "Swapping the client changes where
  the model runs". Left half holds four short bullets: a Microsoft Agent Framework
  Agent owns instructions, tools, sessions and middleware, and the chat client passed
  through client= decides where the model runs, with FoundryChatClient, the two OpenAI
  clients and AnthropicClient as the options. Right half shows the supplied diagram:
  one Agent box with a client slot that fans out to four chat clients and the endpoint
  each one reaches. Clean minimal style, white background, no extra imagery.
---

# Agent and chat client

## Content

- An **Agent** owns instructions, tools, sessions and middleware
- **FoundryChatClient** calls a Foundry deployment through the Responses endpoint
- The two **OpenAI clients** speak the Responses API and Chat Completions
- **AnthropicClient** runs Claude from the beta `agent-framework-anthropic` package

## Speaker Notes

Every agent in this module is two parts: an `Agent` and the chat client it gets through `client=`. In the demo one trail-guide agent runs on four clients and its code stays the same.

Agent: it owns instructions, tools, sessions and middleware, and the model name is always the `model` argument. Agent Framework does not load `.env` on its own, so call `load_dotenv(override=True)` before you build a client.

FoundryChatClient: it calls a model deployed in your Foundry project through `{project_endpoint}/openai/v1/responses`. Install `agent-framework-foundry` rather than the metapackage, which pulls in every optional integration.

OpenAI clients: `OpenAIChatClient` uses the Responses API and is the recommended one because it supports hosted tools. `OpenAIChatCompletionClient` fits OpenAI-compatible endpoints, and in the demo both answer from the same deployment.

AnthropicClient: Claude models run behind it, and it reads `ANTHROPIC_API_KEY` and `ANTHROPIC_CHAT_MODEL`. For Claude deployed in a Foundry resource, use `AnthropicFoundryClient`.
