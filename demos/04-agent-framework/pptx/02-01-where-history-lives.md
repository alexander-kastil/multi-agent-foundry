---
slide: "02-01"
topic: 02-agent-session
title: "Where the history lives"
subtitle: "On the service, or inside the session"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/02-01-where-history-lives.svg
order: 5
source: demos/04-agent-framework/02-agent-session/demo-agent-session.md
gamma_prompt: >
  Slide titled "Where the history lives", subtitle "On the service, or inside the
  session". Left half holds four short bullets on the two storage modes of a Microsoft
  Agent Framework AgentSession: service-managed history, where the session only holds
  a resp_ conversation ID, and local history with store set to False, where every
  message sits in the session state. Right half shows the supplied diagram comparing
  the two saved session files side by side, with the four messages in Foundry on one
  side and inside the session on the other. Clean minimal style, white background,
  no extra imagery.
---

# Where the history lives

## Content

- With **service-managed** history the session only holds the conversation's `resp_` ID
- `FoundryChatClient` and the OpenAI Responses client store history on the service by default
- `options={"store": False}` keeps every message in the session's **state**
- `InMemoryHistoryProvider` writes those messages, so the session grows with every turn

## Speaker Notes

A session looks different depending on where the conversation is stored. The demo saves both kinds to disk, so you can open the two files and compare them.

Service-managed: `service_session_id` points to the conversation on the service and `state` stays empty. The saved `sessions/session.json` holds no messages, only the `resp_` ID.

Default: `FoundryChatClient` and the OpenAI Responses client keep history on the service unless you say otherwise. That is why a plain run produces a session that is only an ID.

Local: passing `options={"store": False}` keeps the transcript in the session instead, and `service_session_id` prints `None`. The serialized session then carries every message.

InMemoryHistoryProvider: it writes the messages under `state`, and the stored count climbs from 2 to 4 after the second turn. That growth is what history providers and compaction manage later in the module.
