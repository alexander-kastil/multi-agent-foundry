---
slide: "07-01"
topic: 07-enterprise-a2a
title: "Capability routing in the registry"
subtitle: "Healthy, fresh and unexpired agents get the work"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/07-01-capability-routing.svg
order: 17
source: demos/06-hosting/07-enterprise-a2a/demo-enterprise-a2a.md
gamma_prompt: >
  Slide titled "Capability routing in the registry", subtitle "Healthy, fresh and unexpired agents get the
  work". Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram in
  which three registry entries built from agent cards feed a query for sector-analysis that checks the
  capability, a healthy status, a heartbeat younger than 120 seconds and a 300 second time to live, then
  routes round robin, next to the four results the lab prints, shown as an inline content image at its
  natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background or side
  accent. Use exactly the one image given for this card; do not generate or add any other images, icons or
  logos.
---

# Capability routing in the registry

## Content

- The registry turns each **agent card** into an entry: capabilities, endpoint and health data
- An entry qualifies when it is `healthy`, its heartbeat is younger than **120 s** and it is within its **300 s** time to live
- Among the candidates, **round robin** over a sorted list keeps the result reproducible
- A `draining` agent stops getting new work, and a crashed one drops out without deregistering

## Speaker Notes

This slide follows lab Step 1: three agent cards become registry entries, and one capability query decides who gets the work.

Cards to entries: capabilities come from the card's skills, the endpoint from its first interface, and the registry adds the health data a card never carries. In the C# version the three cards arrive over HTTP from `/.well-known/agent-card.json`.

Three conditions at once: the status is healthy, the last heartbeat is younger than 120 seconds and the entry has not passed its 300-second time to live. The risk agent never appears, because it offers `portfolio-risk` and not `sector-analysis`.

Round robin: the demo routes over a sorted candidate list, so every run prints the same order. Production registries add latency, region affinity or load to that choice.

Draining and stale: the highlight walks the four results the lab prints. Westeurope sets draining and drops out, then the eastus heartbeat is moved 600 seconds back and no candidate is left.
