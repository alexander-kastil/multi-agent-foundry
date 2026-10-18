---
slide: "03-02"
topic: 03-evaluation-suites
title: "The staged regression gate"
subtitle: "Canaries first, then the full suite, then the baseline comparison"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/03-02-staged-regression-gate.svg
order: 8
source: demos/07-evaluation/03-evaluation-suites/demo-evaluation-suites.md
gamma_prompt: >
  Slide titled "The staged regression gate", subtitle "Canaries first, then the full suite, then the
  baseline comparison". One half holds five short bullets: canaries run first and stop the gate below
  canary_min_rate 0.8, the full suite runs 18 cases plus 3 historical probes, the result is compared
  with baseline.json overall and per interaction type, v2 stops at the canaries with 33 percent while v1
  gets APPROVE DEPLOYMENT, and limits must be wider than the run-to-run spread. The other half shows the
  supplied diagram: a left-to-right pipeline of amber stages from candidate to a green APPROVE
  DEPLOYMENT box, with two red BLOCK exits below. White background, no extra imagery.
---

# The staged regression gate

## Content

- **Canaries** run first and stop the gate below `canary_min_rate` 0.8
- The **full suite** runs 18 cases plus 3 historical probes
- The result is compared with **`baseline.json`**, overall and per interaction type
- `v2` stops at the canaries with 33%, `v1` gets **APPROVE DEPLOYMENT**
- Limits have to be wider than the **run-to-run spread**

## Speaker Notes

regression_gate.py turns the suite and the baseline into a gate that passes or blocks a prompt change, with exit code 0 or 1 for the pipeline.

Canaries first: the script runs the 6 canaries and stops when the completion rate falls below canary_min_rate. A failed canary stage stops the pipeline before the expensive stage.

Full suite: when the canaries pass, the 18 generated cases and the 3 probes run. Per-case rows with the judge's reasoning are written to the results folder.

Baseline comparison: the gate compares task completion, goal achievement and the completion rate per interaction type with baseline.json. A drop in one interaction type counts against its own limit, so a regression hidden in an average still blocks the release.

v1 and v2: v2 approves every request and refunds the list price; it reaches 33% on the canaries and the full suite never runs. v1 reaches 100% on the canaries, every delta stays within its limit and the gate prints APPROVE DEPLOYMENT.

Spread: the agent and the judge are both models, so even the reference version misses a canary now and then. Set each limit from the spread you observe on repeated runs of one version.
