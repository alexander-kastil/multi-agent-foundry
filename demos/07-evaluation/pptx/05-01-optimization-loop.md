---
slide: "05-01"
topic: 05-agent-optimizer
title: "The optimization loop"
subtitle: "Six steps from a baseline version to a promoted candidate"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/05-01-optimization-loop.svg
order: 12
source: demos/07-evaluation/05-agent-optimizer/readme.md
gamma_prompt: >
  Slide titled "The optimization loop", subtitle "Six steps from a baseline version to a promoted
  candidate". Left: four short bullets. Right, wider: the provided diagram of six numbered steps,
  three grey input cards on the left, a purple optimizer box with three stages in the middle and two
  green result cards on the right, shown whole and not cropped. Use exactly the one image given for
  this card; do not generate or add any other images, icons or logos.
---

# The optimization loop

## Content

- Pick a **baseline** version, an evaluation dataset and the evaluators
- The optimizer scores the baseline, proposes **candidates** and scores them on the same data
- The eval model scores answers; the **optimization model** writes the candidates
- Review the ranked list, then **promote** the winner to a new agent version

## Speaker Notes

The agent optimizer replaces hand tuning with measurement, and every run follows the same six steps.

Inputs: you choose the baseline, which for a prompt agent is a version, a dataset of representative tasks and the evaluators. In the lab that is version 1 of the ticket-triage agent, the twenty tickets in tickets.jsonl and builtin.f1_score.

Candidates: the optimizer evaluates the baseline, generates candidates and evaluates them on the same data. The baseline is always in the ranked list, so every candidate is measured against it.

Two models: the eval model scores responses and can be any chat model. The optimization model writes the candidates and must be a gpt-5 family or DeepSeek deployment.

Promotion: applying the winner creates a new prompt agent version. The lab then confirms the gain with its own evaluation of version 2.
