---
slide: "02-02"
topic: 02-routines
title: "Delivery attempts and retries"
subtitle: "Up to five attempts with exponential backoff"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/02-02-delivery-retries.svg
order: 7
source: demos/06-hosting/02-routines/readme.md
gamma_prompt: >
  Slide titled "Delivery attempts and retries", subtitle "Up to five attempts with exponential backoff". The left
  half holds four short bullets on how a Foundry routine delivers a run to its agent: up to five attempts with
  exponential backoff from 5 to 60 seconds; HTTP 408, 429 and 5xx are retried; any other 4xx fails the run at
  once; each attempt has a 30 second timeout that covers acceptance of the background request. The right half
  shows the supplied diagram unchanged: a timeline of five amber attempt circles with growing gaps, a grey note
  about the 30 second timeout, and three outcome boxes (green request accepted, amber retried, grey any other 4xx).
  White background, clean sans-serif type, no extra imagery.
---

# Delivery attempts and retries

## Content

- Delivery makes up to **five attempts** with exponential backoff from 5 to 60 seconds
- HTTP 408, 429 and 5xx are **retried**
- Any other 4xx **fails the run** at once
- Each attempt has a **30 second timeout** that covers acceptance of the background request

## Speaker Notes

Once a trigger fires, delivery to the agent follows its own retry rules. They also explain what a completed run tells you.

Five attempts: delivery makes up to five attempts, with exponential backoff from 5 to 60 seconds between them.

Retried: HTTP 408, 429 and 5xx responses get another attempt after the backoff.

Fails the run: any other 4xx response fails the run at once.

30 second timeout: each attempt's timeout covers acceptance of the background request. A completed run therefore does not guarantee that the agent's own work is finished.
