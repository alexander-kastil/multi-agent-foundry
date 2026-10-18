---
slide: "00-02"
topic: 07-evaluation
title: "Overview"
subtitle: "The path through the seven topics"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/00-02-overview.svg
order: 2
source: demos/07-evaluation/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "The path through the seven topics". Left: five short bullets.
  Right, wider: the provided diagram of seven numbered topic cards in two rows, four blue cards on top
  and three green cards below, joined by arrows, shown whole and not cropped. Use exactly the one image
  given for this card; do not generate or add any other images, icons or logos.
---

# Overview

## Content

- Make agent runs visible with **traces** across services
- Score what the agent did with **evaluators**, then gate releases on a baseline
- Keep scoring **live traffic** and attack the agent with a red team
- Let the **optimizer** tune instructions and tools, and cut cost with routing and caching
- Finish with **incident response**: replay, bisection and a postmortem

## Speaker Notes

This slide walks the path through the module's seven topics once: the top row traces and scores an agent, the bottom row improves it and responds when it breaks.

Traces: topic 01 turns an agent run into OpenTelemetry spans, exports them to Application Insights and follows one request across services with KQL.

Evaluators and gates: topic 02 scores what the agent did with task and tool evaluators. Topic 03 builds a suite with a calibrated judge and a baseline that blocks a regressed prompt.

Live traffic: topic 04 scores a sample of live responses with continuous evaluation and attacks the agent on purpose with cloud red teaming.

Optimizer and cost: topic 05 lets the agent optimizer rewrite instructions and tool descriptions against a dataset. Topic 06 measures routing, caching and token budgets against a tier-2-only baseline.

Incident response: topic 07 diagnoses a bad prompt release, replays and bisects a captured failure and writes a blameless postmortem.
