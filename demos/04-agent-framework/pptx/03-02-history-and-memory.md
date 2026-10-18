---
slide: "03-02"
topic: 03-context-providers
title: "History and memory"
subtitle: "A replayed transcript or facts recalled across sessions"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/03-02-history-and-memory.svg
order: 8
source: demos/04-agent-framework/03-context-providers/readme.md
gamma_prompt: >
  Slide titled "History and memory", subtitle "A replayed transcript or facts
  recalled across sessions". Left half holds four short bullets: a history provider
  replays the transcript stored under one session ID, FileHistoryProvider writes one
  JSON Lines file per session and Redis or Cosmos swap the store, memory extracts
  durable facts, and FoundryMemoryProvider recalls the relevant ones across sessions
  within a scope. Right half shows the supplied two-lane diagram, history on top and
  memory below. Clean minimal style, white background, no extra imagery.
---

# History and memory

## Content

- **History** replays the transcript stored under one session ID
- `FileHistoryProvider` writes one JSON Lines file per session; Redis and Cosmos change the store
- **Memory** extracts durable facts and recalls only the relevant ones
- `FoundryMemoryProvider` shares facts across sessions within one `scope`

## Speaker Notes

History and memory both reach the model through context providers, but they keep different things. Pick by what you need to recall: an exact transcript, extracted facts, or documents.

History: a history provider loads and stores the message transcript, keyed by the session ID. In the demo two runs with the session ID `alex-trip` share one conversation.

FileHistoryProvider: it writes one line per message, so two runs leave four lines in `history/alex-trip.jsonl`. Swapping it for `RedisHistoryProvider` or `CosmosHistoryProvider` changes where the lines live, not the agent code.

Memory: `FoundryMemoryProvider` connects the agent to a Foundry memory store, which extracts facts asynchronously. A fact told in one session can take several seconds before the next session can recall it.

Scope: the `scope` isolates memories, for example per user, and without it the session ID is used. The demo tells session one about Bruno the dog and asks a second session which trail suits you.
