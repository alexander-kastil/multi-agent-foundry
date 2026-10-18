---
slide: "01-02"
topic: 01-prompt-agents
title: "Continuing a conversation on version 2"
subtitle: "History stays in the conversation when the version changes"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/01-02-conversation-across-versions.svg
order: 4
source: demos/03-agent-service/01-prompt-agents/demo-prompt-agents.md
gamma_prompt: >
  Slide titled "Continuing a conversation on version 2", subtitle "History stays in the
  conversation when the version changes". Left half: four short bullets on a seeded
  conversation item, responses appending their input and output, version 2 answering the
  same conversation, and history belonging to the conversation while behavior belongs to
  the version. Right half: the supplied diagram of one stored conversation with seven
  items, answered first by version 1 and then by version 2, shown whole without cropping.
  Clean minimal style, white background.
---

# Continuing a conversation on version 2

## Content

- `conversations.create` seeds the first item: **I bought the Trailhead 2 tent last week**
- Each turn passes `conversation.id`, and every response **appends** its input and output
- Version 2 answers the **same conversation** and still knows about the tent
- History belongs to the **conversation**, behavior belongs to the version

## Speaker Notes

This slide follows one stored conversation through Steps 3 to 6 of the demo. The highlight steps through the items in the order they were added.

Seeded item: the conversation starts with one user message created by conversations.create. Neither later question mentions the tent or the date, so the answers come from that item.

Appended items: each response passes conversation=conversation.id, and its input and output join the list. Reading the items back shows exactly what the next response receives as context.

Version 2: Step 6 sends Summarize what we discussed with a reference to version 2. It answers with two sentences and a closing question, and it still knows about the Trailhead 2 tent.

History and behavior: the conversation and the agent are separate objects, so one conversation can continue on a newer version. The summary proves that the history lives in the conversation and not in an agent version.
