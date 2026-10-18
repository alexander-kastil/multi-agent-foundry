---
slide: "03-01"
topic: 03-evaluation-suites
title: "Generating a suite from a grid"
subtitle: "3 categories x 2 personas x 3 adversarial conditions give 18 cases"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/03-01-suite-from-grid.svg
order: 7
source: demos/07-evaluation/03-evaluation-suites/readme.md
gamma_prompt: >
  Slide titled "Generating a suite from a grid", subtitle "3 categories x 2 personas x 3 adversarial
  conditions give 18 cases". A narrow text column holds five short bullets: a grid makes coverage a
  decision, a model writes one case per combination with a ground_truth, the 6 cases with no
  adversarial condition are the canaries, 3 historical probes join every full run, and the full run
  scores 21 cases. The wider side shows the supplied diagram: a grid of case-000 to case-017 by
  interaction category, persona and adversarial condition with the canary cells in amber, a column of
  three historical probes, and a card with the ground truth of case-001. White background, no extra
  imagery.
---

# Generating a suite from a grid

## Content

- A **grid** makes coverage a decision: 3 x 2 x 3 = 18 combinations
- A model writes one case per combination, each with a **`ground_truth`**
- The 6 cases with no adversarial condition are the **canaries**
- 3 **historical probes** from past incidents join every full run
- The full run scores **21 cases**

## Speaker Notes

Before a gate can pass or block a change, it needs a fixed set of cases, and this suite is generated from a grid rather than collected.

Grid: generate_dataset.py crosses refund_calculation, returns_processing and order_modification with two personas and three adversarial conditions. A useful suite covers combinations, not just happy paths.

Ground truth: every case states what a correct agent must do. For case-001 that is refunding the amount actually paid, 27, not the list price of 36.

Canaries: they are the easy cases with no adversarial condition. If they fail, the full suite is not worth running.

Historical probes: they are cases taken from past incidents and appended to every full run, so a fixed bug stays fixed. historical_probes.json holds three of them.

21 cases: the full run scores the 18 generated cases plus the 3 probes. Running the generator again replaces synthetic_test_suite.json.
