---
slide: "05-01"
topic: 05-task-decomposition
title: "Prompt chain with a gate and a branch"
subtitle: "Code checks the metrics and picks the valuation prompt between agent calls"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/05-01-prompt-chain.svg
order: 11
source: demos/05-orchestration/05-task-decomposition/demo-task-decomposition.md
gamma_prompt: >
  Slide titled "Prompt chain with a gate and a branch", subtitle "Code checks the metrics and
  picks the valuation prompt between agent calls". A narrow text column with four short bullets
  beside a large two-row flow diagram on white. Top row: a grey FILING_TEXT box, a blue Step 1
  metric_extractor box, an amber validate_metrics gate with a red AssertionError box above it
  for out of range values, and a blue Step 2 profile_classifier box. A connector labelled
  "label, here GROWTH_DRIVEN" leads to the bottom row: an amber BRANCHES lookup table mapping
  GROWTH_DRIVEN to five year DCF, MATURE_EARNINGS to comparable multiples and ASSET_HEAVY to
  book value, then a blue Step 3 valuation_analyst box and a blue Step 4 analyst_commentator
  box. Blue marks agent calls, amber marks your code. Clean minimal style, no drop shadows.
---

# Prompt chain with a gate and a branch

## Content

- Agents run in a **fixed order**, with ordinary code between the calls
- **validate_metrics** stops an out-of-range extraction before the next model call
- **BRANCHES** turns the classifier's label into the valuation prompt with a dictionary lookup
- The cheapest and most **predictable** form of decomposition

## Speaker Notes

The first strategy keeps the split in your own code. The demo runs it on the Fabrikam Robotics filing text in four agent steps.

Fixed order: the metric extractor, profile classifier, valuation analyst and commentator run one after another. Because each agent returns JSON, code can check the output before the next step runs.

Validation gate: `validate_metrics` asserts an operating margin between -1.0 and 1.0 and a debt to equity ratio of at least 0. A bad extraction raises an `AssertionError` and never reaches the valuation agent.

Branch: `BRANCHES` maps GROWTH_DRIVEN, MATURE_EARNINGS and ASSET_HEAVY to a valuation prompt. In the demo run the classifier chose GROWTH_DRIVEN, and the table turned that label into a discounted cash flow prompt without another model call.

Predictable: a chain is the cheapest and most predictable form of decomposition. Use it when the steps are known and each output can be checked mechanically.
