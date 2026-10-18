---
slide: "04-01"
topic: 04-foundry-iq
title: "Reasoning effort levels"
subtitle: "What minimal, low and medium spend on one two-hop question"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/04-01-reasoning-effort.svg
order: 11
source: demos/03-agent-service/04-foundry-iq/demo-foundry-iq.md
gamma_prompt: >
  Slide titled "Reasoning effort levels", subtitle "What minimal, low and medium spend on one
  two-hop question". Four short bullets on the left, a large diagram on the right: a three-row
  grid comparing the effort levels minimal, low and medium across query planning, search, output
  and one measured run. Minimal makes no model call and returns extracted content; low and medium
  let gpt-5.4 plan subqueries and return an answer with citations. White background, amber effort
  labels, purple model cells, green output cells, clean sans-serif type.
---

# Reasoning effort levels

## Content

- **minimal** sends `intents`, makes no model call and returns extracted passages
- **low** lets `gpt-5.4` decompose the question and write an answer with `ref_id` citations
- **medium** spends more reasoning on planning and iteration
- **Higher effort** answers richer questions and costs more tokens and time

## Speaker Notes

The demo knowledge base answers the same two-hop question at three effort levels. Here the three runs from Step 3 sit side by side, so you can see what each level pays for.

Minimal: nothing plans the retrieval, so the request carries `intents` instead of `messages` and every source is searched with the question as written. The run took 4.2 seconds, made two index searches and returned raw passages with zero planning and synthesis tokens.

Low: `gpt-5.4` splits the question into subqueries, runs them in parallel with hybrid search and semantic reranking, and writes an answer with `ref_id` citations. That run made six index searches and used 1465 planning and 2201 synthesis tokens in 7.3 seconds.

Medium: the same pipeline with more reasoning on planning and iteration. For a question this size it added little and took 12.9 seconds, almost twice as long as low.

Higher effort: it plans more and handles richer questions, and you pay in tokens and time. The demo numbers come from one run and change from run to run.
