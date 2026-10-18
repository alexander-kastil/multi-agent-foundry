---
slide: "06-03"
topic: 06-resilient-orchestration
title: "Quorum over parallel spokes"
subtitle: "Two of three answers are enough"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/06-03-quorum-parallel-spokes.svg
order: 16
source: demos/05-orchestration/06-resilient-orchestration/demo-resilient-orchestration.md
gamma_prompt: >
  Slide titled "Quorum over parallel spokes", subtitle "Two of three answers are enough". Two columns.
  Left column: the four bullets. Right column, wider than the left: the provided diagram of three cases
  side by side, each with three spokes marked success or timeout, a row of filled slots against an amber
  quorum line at required=2, and the resulting status, green success twice and amber insufficient_quorum
  once, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column,
  never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Quorum over parallel spokes

## Content

- `parallel_research` calls three spokes at once with `asyncio.gather`
- `required=2` sets the **quorum**: two successes out of three are enough
- One timed-out spoke still gives `success` and is listed in `missing`
- Below the quorum the status is `insufficient_quorum`, with no answer made up

## Speaker Notes

When spokes are independent, the hub can call them all at once and decide how many answers are enough. The three columns are the three cases the quorum stage of the demo runs.

Parallel calls: `asyncio.gather` with `return_exceptions=True` collects every outcome, and each spoke gets its own timeout from the `timeouts` dictionary. In C# `Task.WhenAll` does the same.

The quorum: the caller passes `required=2`, and the function counts the successes against it. With all three spokes healthy the status is `success` and `missing` is empty.

One spoke short: ESG times out after 0.01 seconds, and market analysis and risk assessment still form a quorum. The result is `success` and names `esg_evaluation` as missing, so a slow optional spoke can be cut off early.

Below the quorum: when market analysis and ESG both time out, one answer is left and the status is `insufficient_quorum`. The function refuses to answer from a single opinion.
