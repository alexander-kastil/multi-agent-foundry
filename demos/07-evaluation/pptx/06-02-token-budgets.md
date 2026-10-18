---
slide: "06-02"
topic: 06-performance-cost
title: "Token budgets and context assembly"
subtitle: "A fixed layer order and a fixed pruning order"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/06-02-token-budgets.svg
order: 16
source: demos/07-evaluation/06-performance-cost/demo-performance-cost.md
gamma_prompt: >
  Slide titled "Token budgets and context assembly", subtitle "A fixed layer order and a fixed pruning
  order". Left: four short bullets. Right, wider: the provided diagram of two stacks of five context
  layers side by side, the right stack with three numbered amber pruning tags, above three horizontal
  token bars, shown whole and not cropped. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Token budgets and context assembly

## Content

- Each agent type gets a **token budget**
- The context is assembled from layers in a **fixed order**
- Over budget, the assembler **prunes in order**: window, important turns, tool results
- The naive prompt is **1,706 tokens**; the reasoning agent needs 185

## Speaker Notes

Every token in the prompt is paid for on every call, so the lab gives each agent type a budget and assembles its context instead of resending the whole conversation.

Budgets: AGENT_BUDGETS sets 2000 tokens for lookup, 4000 for reasoning and 8000 for policy. The lab squeezes the lookup agent to 120 so that pruning has to happen.

Fixed order: the context holds the system prompt, a digest of established facts, tool results compressed to the fields the agent needs, important older turns and the last three exchanges. A lookup agent does not need a 78-field product record.

Pruning order: only when the result is still over budget does the assembler prune, first summarizing the window, then dropping the important turns, then the tool results. The lookup agent at 120 went through all three steps and ended at 94 tokens.

Numbers: the naive prompt is 1,706 tokens, and the reasoning agent's assembled context is 185 tokens with nothing pruned. Counting with the model's tokenizer instead of characters keeps the budget honest.
