---
slide: "03-01"
topic: 03-orchestration-patterns
title: "Three orchestration builders"
subtitle: "Pass in agents, get back an ordinary workflow"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/03-01-orchestration-builders.svg
order: 7
source: demos/05-orchestration/03-orchestration-patterns/readme.md
gamma_prompt: >
  Slide titled "Three orchestration builders", subtitle "Pass in agents, get back an ordinary
  workflow". Left half holds four short bullets, one per builder plus one on what they return.
  Right half shows the supplied diagram uncropped: three panels side by side, SequentialBuilder
  with writer, reviewer and polisher in a column, ConcurrentBuilder with finance, security and
  compliance in parallel feeding an aggregator, and HandoffBuilder with a user above triage_agent,
  which links to billing_agent and technical_agent. Agents are purple, results green. White
  background, clean minimal style.
---

# Three orchestration builders

## Content

- **SequentialBuilder** runs writer, reviewer and polisher in list order; each sees the earlier conversation
- **ConcurrentBuilder** sends one proposal to finance, security and compliance at once, then aggregates
- **HandoffBuilder** lets `triage_agent` pass the conversation to `billing_agent` or `technical_agent`
- Each builder returns an ordinary `Workflow` with events, outputs, checkpoints and requests

## Speaker Notes

Most multi-agent designs are a pipeline, a panel or a front desk. Agent Framework ships each shape as a builder, so you pass in agents and get back a ready workflow.

Sequential: the participant order is the execution order, and by default each agent receives the whole earlier conversation. The polisher's final text contains the reviewer's call to action because the polisher saw the whole conversation.

Concurrent: every participant gets the same input at once, and wall-clock time is that of the slowest participant. `with_aggregator` swaps the default aggregator for your own reduction, in the demo a summarizer agent.

Handoff: the start agent transfers the conversation by calling a handoff tool that the builder registers, and the receiving agent takes ownership. There is no central manager; `add_handoff` decides who may hand off to whom.

Ordinary workflows: each builder returns a `Workflow`, so streaming events, outputs, checkpoints and human requests from the earlier topics all apply. When the steps and branches are known up front, a hand-built `WorkflowBuilder` graph is simpler and cheaper.
