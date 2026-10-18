---
slide: "05-02"
topic: 05-agent-optimizer
title: "Candidate scores"
subtitle: "Improvement bands and tokens per answer"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/05-02-candidate-scores.svg
order: 13
source: demos/07-evaluation/05-agent-optimizer/demo-agent-optimizer.md
gamma_prompt: >
  Slide titled "Candidate scores", subtitle "Improvement bands and tokens per answer". Left: five
  short bullets. Right, wider: the provided chart with amber bars for three candidates over four green
  reading bands on the left and a second bar chart of tokens per answer on the right, shown whole and
  not cropped. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Candidate scores

## Content

- Every candidate is scored from **0.0 to 1.0**, next to the baseline
- An improvement below 0.03 is **noise**; 0.10 to 0.20 is significant
- candidate_1 gained **0.105** over a baseline of 0.012
- Its answers used **328 tokens** against 134 for the baseline
- **Repeat the job** before you trust one outcome

## Speaker Notes

A score alone hides the cost, so this slide reads the candidates of the lab's Python run next to their tokens per answer.

Score range: the optimizer rescales every evaluator's score to 0.0 to 1.0, flips evaluators where lower is better and averages across tasks. The baseline is always in the list.

Bands: below 0.03 is noise, 0.03 to 0.10 is moderate and worth deploying, 0.10 to 0.20 is significant and above 0.20 is major, usually from a poor baseline. candidate_1 gained 0.105, while candidates 2 and 3 gained 0.015 and 0.014, which is noise.

The winner: candidate_1 scored 0.117 against a baseline of 0.012 and also answered faster. It is the one marked with a star in the show_candidates output.

Token cost: optimized instructions are often longer, and candidate_1 used about two and a half times the tokens of the baseline. You decide whether that increase is proportional to the gain.

Repeat: the job is not deterministic, and another run on the same agent reached 0.42 from a baseline of 0.016. Repeat it or raise max_candidates before you rely on a single outcome.
