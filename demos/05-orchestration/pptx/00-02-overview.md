---
slide: "00-02"
topic: 05-orchestration
title: "Overview"
subtitle: "The path through the module"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/00-02-overview.svg
order: 2
source: demos/05-orchestration/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "The path through the module". Two columns. Left column: the bullets.
  Right column, wider than the left: the provided diagram of seven numbered topic cards in reading order,
  four in the first row and three in the second, joined by arrows, with a grey card for the Microsoft
  Agent Framework 1.x workflow engine, shown as an inline content image at its natural 16:9 aspect ratio,
  scaled to fit the column, never cropped, never used as a background or side accent. Use exactly the one
  image given for this card; do not generate or add any other images, icons or logos.
---

# Overview

## Content

- **Workflows**: executors, edges and supersteps, then checkpoints and human requests
- **Orchestration builders** for sequential, concurrent, handoff, group chat and Magentic patterns
- **Task decomposition** with a planner whose dependency graph runs in waves
- **Resilience** with status envelopes, a circuit breaker, a quorum and a supervisor
- **Declarative YAML and functional workflows**, and the migration of portal workflows

## Speaker Notes

This slide walks the seven topics once: the first four build up the workflow engine and its builders, and the last three apply them.

Workflows and checkpoints: typed graphs of executors and edges run in supersteps. Checkpoints and human requests let a run pause for a person and resume in another process.

Builders: sequential, concurrent and handoff come ready-made, and group chat and Magentic put a manager in the middle that decides who speaks next.

Task decomposition: a prompt chain with validation gates, a planner agent that builds a dependency graph executed in waves, and handoff collaboration with turn limits.

Resilience and declarative workflows: a research hub keeps running when spokes time out, fail or return a weak report. The last topic loads YAML with `WorkflowFactory`, writes `@workflow` and `@step` in plain Python and migrates Foundry portal workflows before the designer retires on December 1, 2026.
