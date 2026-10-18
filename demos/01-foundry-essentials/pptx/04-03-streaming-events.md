---
slide: "04-03"
topic: 04-responses-api
title: "Streaming event sequence"
subtitle: "Only response.output_text.delta carries text"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/04-03-streaming-events.svg
order: 13
source: demos/01-foundry-essentials/04-responses-api/demo-responses-conversations.md
gamma_prompt: >
  Slide titled "Streaming event sequence", subtitle "Only response.output_text.delta
  carries text". Left half holds five short bullets: stream=True returns server-sent
  events instead of a finished response; every added event has a matching done event,
  nested from response to output item to content part; only the text delta events carry
  text; response.completed carries the usage and the C# SDK delivers each event as a
  typed update class; in the demo run the first token arrived after 1.75 seconds and the
  answer completed after 2.63 seconds. Right half shows the supplied diagram listing the
  nine event types in order with their .NET names and a timing bar below. Clean and
  light, white background.
---

# Streaming event sequence

## Content

- `stream=True` returns **server-sent events** instead of a finished response
- Every **added** event has a matching **done** event, nested from response to output item to content part
- Only **`response.output_text.delta`** carries text; a client can ignore the other types
- `response.completed` carries the **usage**; C# delivers each event as a typed update class
- First token after **1.75 s**, completed after 2.63 s in the demo run

## Speaker Notes

With the history settled, this slide covers how the answer reaches the user. It lists the events of one streamed text response in the order they arrive.

Events instead of a response: with `stream=True` the call returns server-sent events, and you react only to the types you care about. Streaming combines with conversations, so a streamed answer is still appended when it completes.

Pairs of added and done: the response opens an output item, the item opens a content part, and each closes with its done event before `response.completed`. The brackets on the slide show those pairs.

Only deltas carry text: the client prints `event.delta` for `response.output_text.delta` and ignores the rest. The C# command lists the same nine events as typed classes such as `OutputTextDelta`.

Usage and timing: the code reads `event.response.usage` from `response.completed`. In the demo run the first token arrived at 1.75 seconds and the answer completed at 2.63 seconds, and that gap is the latency a user does not wait through.
