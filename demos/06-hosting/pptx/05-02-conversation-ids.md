---
slide: "05-02"
topic: 05-self-hosting
title: "Continuing a conversation by id"
subtitle: "previous_response_id changes every turn, a conversation id stays the same"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/05-02-conversation-ids.svg
order: 13
source: demos/06-hosting/05-self-hosting/demo-self-hosting.md
gamma_prompt: >
  Slide titled "Continuing a conversation by id", subtitle "previous_response_id changes
  every turn, a conversation id stays the same". Left half holds five short bullets on the
  two ways the Responses protocol continues a chat, that they are mutually exclusive, that
  the server loads, runs and saves the session under the id, and that an id does not prove
  the caller owns the session. Right half shows the supplied two-panel diagram: on the left
  two turns linked by previous_response_id with a new session key per turn, on the right
  two turns that share one conversation id and one session. Clean minimal style, white
  background, no extra imagery.
---

# Continuing a conversation by id

## Content

- `previous_response_id` points at the **last response**, so the id changes every turn
- `conversation` names a **stable** `conv_` id for every turn
- A request carries one of the two, they are **mutually exclusive**
- The server **loads, runs and saves** the session under that id
- An id is **not proof of ownership**: authenticate and authorize first

## Speaker Notes

The Responses protocol continues a chat in one of two ways, and the demo uses one per host. Both end in the same server-side step: load the session, run the new input, save.

previous_response_id: in Step 3 the Python host saves the session under the first response id, and the second call passes that id as `previous_response_id`. The server loads that session and saves the result under the next response id.

conversation: in Step 5 the C# host creates a `conv_` id with `POST /v1/conversations`, and both turns send it. The first turn calls `lookup_ticket`, the second answers from the conversation without calling the tool again.

Mutually exclusive: a request carries either a previous response id or a conversation id, never both.

Load, run, save: the server looks up the saved session by the id, runs the new input in it and saves the session again. An id the server never saw starts a fresh session instead of failing.

Not proof of ownership: a response id or conversation id identifies a session, it does not prove the caller owns it. Authenticate the caller, authorize access to the referenced state and partition stored sessions by tenant or user before you trust either id.
