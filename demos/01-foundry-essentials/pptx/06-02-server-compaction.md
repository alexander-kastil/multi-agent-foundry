---
slide: "06-02"
topic: 06-context-engineering
title: "Server-side compaction in a loop"
subtitle: "The window drops back once it crosses compact_threshold"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/06-02-server-compaction.svg
order: 17
source: demos/01-foundry-essentials/06-context-engineering/demo-context-engineering.md
gamma_prompt: >
  Slide titled "Server-side compaction in a loop", subtitle "The window drops back once it
  crosses compact_threshold". Left half holds four short bullets: every request carries
  context_management with a compact_threshold of 6,000 tokens and needs no extra call;
  day 11 sends 7,024 input tokens and its output holds a compaction item before the
  message; the loop drops everything before the latest compaction item, so the window
  falls from 40 items to 2; from day 12 the window grows again and day 14 still knows the
  booking reference. Right half shows the supplied bar chart of input tokens for days 9 to
  14 with the threshold line. Clean and light, white background.
---

# Server-side compaction in a loop

## Content

- `context_management` with a **`compact_threshold`** of 6,000 tokens on every request, no extra call
- Day 11 sends **7,024** input tokens, and its output holds a `compaction` item before the message
- The loop **drops everything before the latest compaction item**: the window falls from 40 items to 2
- From day 12 the window grows again at the same rate, and day 14 still knows **ZX-4417**

## Speaker Notes

Step 4 of the demo lets the service compact during an agent loop, so you never call the compaction endpoint yourself. The bars are the input tokens of days 9 to 14.

One parameter: `server_compaction.py` adds `context_management` with type `compaction` and a `compact_threshold` of 6,000 to a normal `responses.create`. The service compacts only when the context crosses that threshold.

Crossing the line: day 10 sends 5,642 tokens and day 11 sends 7,024, so the day 11 output contains a `compaction` item next to the message. This works with `store=False`.

The pruning rule: after each response the loop appends the output items and drops everything before the latest `compaction` item, the documented rule for stateless chaining. With `previous_response_id` you send only the new user message and do not prune by hand.

Growing again: day 12 starts from 1,847 input tokens and the window grows at the same rate as before. The day 14 answer still carries the booking reference ZX-4417 and the sister Marta.
