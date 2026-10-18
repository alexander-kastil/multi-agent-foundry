---
slide: "05-01"
topic: 05-self-hosting
title: "The self-hosted Responses route"
subtitle: "Framework helpers convert the request, your code decides what reaches the agent"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/05-01-responses-route.svg
order: 12
source: demos/06-hosting/05-self-hosting/readme.md
gamma_prompt: >
  Slide titled "The self-hosted Responses route", subtitle "Framework helpers convert the
  request, your code decides what reaches the agent". Left half holds four short bullets:
  two helper functions convert the request and the result, an allowlist in your code
  passes only max_tokens, sessions are loaded and saved by id, and the agent and its tool
  run inside your own server. Right half shows the supplied diagram of one request
  travelling through a FastAPI server, with framework helpers in grey, your code in blue,
  your allowlist in amber and the Foundry model in purple. Clean minimal style, white
  background, no extra imagery.
---

# The self-hosted Responses route

## Content

- `responses_to_run` and `responses_from_run` **convert** the request and the result
- Your **allowlist** passes only `max_tokens`, so `temperature` never reaches the model
- `AgentState` loads and saves the **session** by response or conversation id
- The agent and its `lookup_ticket` tool **run in your process**

## Speaker Notes

Self-hosting puts the agent inside a web server you operate. This slide follows one request through the Python route in `app.py` and colours each step by who owns it.

Helpers convert: `responses_to_run` turns the request body into messages and options, and `responses_from_run` turns the run result back into a Responses payload. The hosting packages ship these helpers, not a server.

Your allowlist: only the options your code lists reach the agent run, here `max_tokens`. A caller can send `tools`, `temperature` or a different model in the body, and the allowlist keeps them out.

Sessions by id: `AgentState` loads the session for the incoming id, the agent runs in it, and the route saves it again. Python keeps sessions in a process-local `SessionStore` by default; for production, back it with Redis, a database or blob storage.

Runs in your process: the `support-desk` agent calls the Foundry model, and its `lookup_ticket` tool runs inside your server. In the demo the tool call shows up in the response next to the final message.
