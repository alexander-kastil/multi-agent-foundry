---
slide: "01-03"
topic: 01-prompt-agents
title: "Carrying history between turns"
subtitle: "A conversation, a previous response or your own items"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/01-03-chaining-turns.svg
order: 5
source: demos/03-agent-service/01-prompt-agents/readme.md
gamma_prompt: >
  Slide titled "Carrying history between turns", subtitle "A conversation, a previous
  response or your own items". Left half: four short bullets on a response running with or
  without a conversation, passing a conversation id, chaining with previous_response_id,
  and resending earlier items yourself. Right half: the supplied three-column diagram that
  compares where the history lives, what the next call passes and what the response gets,
  shown whole without cropping. Clean minimal style, white background.
---

# Carrying history between turns

## Content

- A response runs with or without an agent, and with or without a **conversation**
- Pass `conversation=<id>` and the whole **conversation** becomes input, with the output appended
- Pass `previous_response_id` to chain the turn to a **stored response**
- Or resend the earlier items **yourself** and keep nothing on the server

## Speaker Notes

The next turn needs the earlier turns from somewhere, and a conversation is only one of three places. This slide sets the three options side by side.

Conversation: the server stores message, tool call and tool output items. A response that passes conversation=<id> gets the whole conversation as model input and appends its output to it.

Previous response: the readme asks for the largest city in France, then passes previous_response_id=first.id with the follow-up question. The follow-up only makes sense because the call is chained to the stored response.

Your own items: you can resend the earlier items yourself and keep nothing on the server. Your code then holds the history.

Choosing: start with a response, then add an agent for reusable behavior, a conversation for history, or both. The demo uses a conversation because its history has to outlive a version change.
