---
slide: "06-01"
topic: 06-agent-memory
title: "The three memory phases"
subtitle: "How a conversation becomes memories and how search brings them back"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/06-01-three-memory-phases.svg
order: 16
source: demos/03-agent-service/06-agent-memory/demo-agent-memory.md
gamma_prompt: >
  Slide titled "The three memory phases", subtitle "How a conversation becomes memories and how
  search brings them back". Four short bullets on the left, a large left-to-right diagram on the
  right: a conversation with a preference, an allergy and a correction flows into a memory store
  through extraction and consolidation, lands in one user scope as user profile, chat summary and
  procedural memories, and comes back through a static and a contextual search. White background,
  blue conversation, purple memory store, amber phases, green search results.
---

# The three memory phases

## Content

- **Extraction** pulls candidate facts out of the conversation
- **Consolidation** merges duplicates and resolves conflicts
- One conversation produced all three types: `user_profile`, `chat_summary` and `procedural`
- **Retrieval** returns the static profile for a scope, or contextual memories for the latest messages

## Speaker Notes

Steps 2 and 3 of the demo write one conversation for alice into the store `m3-memory-store` and then search it. The diagram follows that path through the three phases of memory.

Extraction: the store's chat model, `gpt-5.4`, reads the conversation and pulls out candidate facts. Alice's conversation holds a preference, an allergy and a correction of the release-notes process.

Consolidation: duplicates are merged and conflicts resolved, so a new allergy replaces the old note. `begin_update_memories` runs both phases, and `update_delay=0` starts them right away.

Three types: the run created three user profile memories, two chat summaries and one procedural memory. The procedural memory came from the moment the user corrected the assistant.

Retrieval: `search_memories` with only a scope returns the static user profile, and with `items` set to the latest message it returns the most relevant memories of every type. A request to publish the release notes for 2.6 brings back the release routine and the procedural rule.
