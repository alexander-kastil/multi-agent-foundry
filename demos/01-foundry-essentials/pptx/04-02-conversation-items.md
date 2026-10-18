---
slide: "04-02"
topic: 04-responses-api
title: "Conversations and their items"
subtitle: "The service prepends the stored items and appends the new turn"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/04-02-conversation-items.svg
order: 12
source: demos/01-foundry-essentials/04-responses-api/demo-responses-conversations.md
gamma_prompt: >
  Slide titled "Conversations and their items", subtitle "The service prepends the stored
  items and appends the new turn". Left half holds four short bullets: a conversation is
  a durable object that stores messages, tool calls, tool outputs and model output; the
  service prepends the stored items to the input of every response that references it;
  after the response completes it appends the new input and output items; items.create
  adds up to 20 items by hand. Right half shows the supplied diagram of a conversation
  with six items beside one turn with numbered prepend, answer and append steps. Clean
  and light, white background.
---

# Conversations and their items

## Content

- A **conversation** is a durable object that stores items: messages, tool calls, tool outputs and model output
- The service **prepends** the stored items to the input of every response that references it
- After the response completes, it **appends** the new input and output items
- `items.create` adds items by hand, up to 20 per call

## Speaker Notes

The previous slide named the conversation as one place for the history; this one shows what happens inside it during a single turn. The left column is the conversation that `conversation.py` builds in step 3 of the demo.

Items, not text: a conversation stores messages, tool calls, tool outputs and the output the model produced. It has its own id, so you can reuse it across sessions and inspect it for debugging.

The model reads the history first: when a response names the conversation, the service puts the stored items in front of the new input. That is how the model knew the departure city was Vienna.

The new turn is saved after completion: the service appends the question and the answer once the response completes. The two questions in the demo produce items 2 to 5.

Manual items: `items.create` appends up to 20 items per call, which is how "Add Linz as a second stop." became item 6. When the history outgrows the model's context size the model truncates what it reads, but the conversation itself is not truncated.
