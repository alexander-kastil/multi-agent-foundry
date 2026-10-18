---
slide: "06-03"
topic: 06-context-engineering
title: "Prompt caching and the stable prefix"
subtitle: "A cache hit needs an identical first 1,024 tokens"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/06-03-stable-prefix.svg
order: 18
source: demos/01-foundry-essentials/06-context-engineering/demo-context-engineering.md
gamma_prompt: >
  Slide titled "Prompt caching and the stable prefix", subtitle "A cache hit needs an
  identical first 1,024 tokens". Left half holds five short bullets: prompt caching reuses
  the computation for a prefix the service has seen and does not change the answer; a
  hit needs at least 1,024 tokens whose first 1,024 match an earlier request; calls 2 and
  3 read 1,792 cached tokens, 1,024 plus six 128-token steps; with the question first the
  first 1,024 tokens differ and nothing is cached; put stable content first. Right half
  shows the supplied diagram of three requests drawn to scale in tokens with the cached
  part below. Clean and light, white background.
---

# Prompt caching and the stable prefix

## Content

- Prompt caching **reuses** the computation for a prefix the service has seen, and the answer stays the same
- A hit needs at least **1,024 tokens**, and the first 1,024 must match an earlier request
- Calls 2 and 3 read **1,792** cached tokens: 1,024 plus six 128-token steps
- With the **question first**, the first 1,024 tokens differ and nothing is cached
- Put **stable content first**: instructions, tools and reference text before the user turn

## Speaker Notes

Compaction shrinks what you send; prompt caching makes what you keep cheaper. The bars are the requests of `caching.py` in step 5 of the demo, drawn to scale in tokens.

The same answer, cheaper: prompt caching is on by default for supported models, and cache reads are billed at a discount on input tokens. The demo uses one `prompt_cache_key` for every call and `prompt_cache_retention="24h"`.

The rule: a request can hit the cache when it is at least 1,024 tokens long and its first 1,024 tokens are identical to an earlier request. Call 1 writes the cache, so it shows 0 cached tokens.

Steps of 128: calls 2 and 3 read 1,792 of about 2,000 tokens from the cache, which is the 1,024-token minimum plus six 128-token steps. That step size applies to GPT-5.5 and earlier models.

One change in front: the last request has the same key and the same handbook, but the question comes first, so the first 1,024 tokens differ and `cached_tokens` is 0. A single changed character in that range is enough for a miss.

Stable content first: instructions, tool definitions and reference text belong before the user turn, and the conversation should be append-only. A model router deployment does not guarantee hits, because consecutive requests can land on different models.
