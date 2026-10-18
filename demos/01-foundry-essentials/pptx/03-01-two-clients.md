---
slide: "03-01"
topic: 03-foundry-sdk
title: "Project client and OpenAI-compatible client"
subtitle: "Two clients from one endpoint and one credential"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/03-01-two-clients.svg
order: 8
source: demos/01-foundry-essentials/03-foundry-sdk/readme.md
gamma_prompt: >
  Slide titled "Project client and OpenAI-compatible client", subtitle "Two clients from
  one endpoint and one credential". Left half holds four short bullets: AIProjectClient
  takes the project endpoint and DefaultAzureCredential once; the project client handles
  deployments, connections, project properties and tracing; the OpenAI-compatible client
  handles the Responses API, conversations, agents, evaluations and model calls; you get
  it from the project client with get_openai_client() in Python or
  GetProjectOpenAIClient() in C#. Right half shows the supplied diagram of the shared
  configuration, the two clients and the Foundry project. Clean and light, white
  background.
---

# Project client and OpenAI-compatible client

## Content

- `AIProjectClient` takes the **project endpoint** and `DefaultAzureCredential` once
- The **project client** handles deployments, connections, project properties and tracing
- The **OpenAI-compatible client** handles the Responses API, conversations, agents, evaluations and model calls
- You get it from the project client: `get_openai_client()` in Python, `GetProjectOpenAIClient()` in C#

## Speaker Notes

Code that talks to a model has to reach the project with the right identity and speak a request format the model understands. The Foundry SDK covers the first job, the OpenAI-compatible API the second.

Configured once: AIProjectClient takes the project endpoint and a DefaultAzureCredential. There is no API key anywhere in the demo scripts.

Project client: it covers Foundry-native operations that have no OpenAI equivalent, such as deployments, connections, project properties and tracing.

OpenAI-compatible client: it covers anything that follows OpenAI concepts, from the Responses API and Chat Completions to conversations, agents, evaluations and model calls.

One from the other: you get the second client from the first, so authentication and the endpoint are configured once. Most applications use both.
