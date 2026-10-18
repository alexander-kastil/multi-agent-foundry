---
slide: "06-01"
topic: 06-performance-cost
title: "Routing with a multi-level cache"
subtitle: "Where each of the six demo requests was answered"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/06-01-routing-cache.svg
order: 15
source: demos/07-evaluation/06-performance-cost/demo-performance-cost.md
gamma_prompt: >
  Slide titled "Routing with a multi-level cache", subtitle "Where each of the six demo requests was
  answered". Left: five short bullets. Right, wider: the provided diagram of six numbered requests
  beside a flow through two amber cache boxes, a blue router and three purple tier boxes, with two
  green hit boxes, shown whole and not cropped. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Routing with a multi-level cache

## Content

- The **result cache** matches the exact normalized request
- The **semantic cache** matches a paraphrase at similarity 0.85 or higher
- The router sends the rest to the **cheapest tier** that can handle it
- An answer below its tier's **quality floor** retries one tier up
- Four model calls instead of six, about **30% less spend**

## Speaker Notes

This slide follows the six requests of the lab's routing section through the caches and the router, in the order the code checks them.

Result cache: request 5 repeats request 1 word for word, so the result cache answers it without a model call. It is a key-value lookup on the normalized request.

Semantic cache: request 6 rewords the same question and hits at similarity 0.86 against a threshold of 0.85. The threshold is a quality knob: raise it when a wrong cached answer is expensive.

Router: classify_complexity sends short lookups to tier 1, requests with escalation signals such as a legal threat to tier 3 and the rest to tier 2. In the run, requests 1 and 2 went to gpt-5-mini, request 3 to gpt-5.4 and request 4 to gpt-5.2.

Quality floor: the floors are 75, 85 and 60, and an answer below its tier's floor is retried one tier up instead of shipped. A cheap model that answers badly costs more than it saves.

Savings: four model calls replaced six, and spend fell by about 30% against the tier-2-only baseline. Every entry is tagged with order-database, so one invalidation call removed 8 entries.
