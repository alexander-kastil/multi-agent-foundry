---
slide: "05-03"
topic: 05-agent-optimizer
title: "Tool description tuning"
subtitle: "A code-based evaluator reads the function call itself"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/05-03-tool-description-tuning.svg
order: 14
source: demos/07-evaluation/05-agent-optimizer/demo-agent-optimizer.md
gamma_prompt: >
  Slide titled "Tool description tuning", subtitle "A code-based evaluator reads the function call
  itself". Left: five short bullets. Right, wider: the provided diagram of a loop of six cards in two
  rows, queries, prompt agent and function call on top, best candidate, optimization model and an amber
  code-based evaluator below, shown whole and not cropped. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Tool description tuning

## Content

- Twelve queries each expect **one of four tools** with vague descriptions
- A prompt agent's tools are **not executed** during optimization
- A **code-based evaluator** scores the call: 0.0, 0.5 or 1.0
- The optimizer rewrites descriptions; names, types and required fields stay
- **Read every rewrite** before you promote it

## Speaker Notes

Instructions are one target and function tool descriptions are another; this slide shows how the lab tunes the second.

Setup: m7-tool-agent has four tools with deliberately useless descriptions, op_a1 to op_c3. Twelve queries each expect one of them: order status, customer account data, product search or store hours.

No execution: built-in tool evaluators skip a response that is only a function call, because a prompt agent's tools are not executed during optimization. The score has to come from the call itself.

Code-based evaluator: m7_tool_choice compares the chosen tool and its main argument with ground_truth. It returns 0.0 for another tool, 0.5 for the expected tool and 1.0 when the argument matches as well.

Rewrites: the optimization model may change descriptions but not names, types or required fields. In the Python run the score rose from 0.33 to 0.50.

Review: the optimizer never sees what a tool does, only the queries, the agent's calls and the scores, so it described the account lookup op_a2 as a store-hours directory. Read every new description before you promote it.
