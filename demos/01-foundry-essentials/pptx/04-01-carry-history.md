---
slide: "04-01"
topic: 04-responses-api
title: "Three ways to carry history"
subtitle: "What turn 2 sends and where the earlier turns live"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/04-01-carry-history.svg
order: 11
source: demos/01-foundry-essentials/04-responses-api/readme.md
gamma_prompt: >
  Slide titled "Three ways to carry history", subtitle "What turn 2 sends and where the
  earlier turns live". Left half holds four short bullets: a model call is stateless, so
  you choose where the history lives; previous_response_id sends the new input plus the
  id of the last stored response; a conversation id points at one durable object on the
  service; client-side history resends every earlier message with store=False and leaves
  nothing in the service. Right half shows the supplied diagram with three rows that
  compare the request on turn 2 and where the history lives. Clean and light, white
  background.
---

# Three ways to carry history

## Content

- A model call is **stateless**: you decide where the history of turn 1 lives
- **`previous_response_id`**: send the new input and the id of the last response, which is stored by default
- **Conversation**: send the conversation id; the service keeps one durable object with its items
- **Client-side** with `store=False`: resend every earlier message; the service keeps nothing

## Speaker Notes

This slide frames the whole topic: before streaming, you decide where the history of a multi-turn exchange lives. The three rows show the same second turn sent three ways.

Stateless calls: to make the second turn aware of the first, something has to carry the first turn along. The three rows are the three choices from the table in the readme.

Chained by id: responses are stored by default, so the second call sends only the new question and `previous_response_id=first.id`. The service finds the history through the stored response.

One durable object: the request carries the conversation id and the new input, and the service keeps the items. You can reuse that object across sessions and let several responses append to it.

You own the list: with `store=False` the service keeps nothing, so your code resends every earlier message in `input`. Step 2 of the demo shows both answers are right and that you pay for the history differently.
