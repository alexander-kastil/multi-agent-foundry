---
slide: "07-02"
topic: 07-agentic-loops
title: "Plan, reflect and fork on one response chain"
subtitle: "Each turn names the response before it"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/07-02-response-chain.svg
order: 19
source: demos/03-agent-service/07-agentic-loops/demo-agentic-loops.md
gamma_prompt: >
  Slide titled "Plan, reflect and fork on one response chain", subtitle "Each turn names the response before
  it". Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram in three
  rows, a JSON plan then a check in your code then the executed plan, three reflection cycles with falling
  confidence and a stop rule, and two background branches named bull and bear that start from the same
  response, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never
  cropped, never used as a background or side accent. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Plan, reflect and fork on one response chain

## Content

- **Plan then act**: a JSON plan first, then a second turn that executes it
- Between the two turns your code can add validation, approval or cost limits
- Reflection stops at **confidence 8** or after three cycles, whichever comes first
- Two **background branches** start from the same response id and never see each other

## Speaker Notes

This slide follows one analyst run through three of the four loop designs, all linked by previous_response_id.

Plan then act: the first turn asks only for a JSON plan, and the recorded run printed six steps. The second turn is chained to the plan, so the agent executes a plan it can see.

The gap between the turns: your code parses the plan with json.loads before anything runs. That is where you add validation, human approval or cost limits.

Two stopping conditions: each cycle critiques and revises the analysis and returns a confidence score. In the recorded run the confidence fell from 6 to 5 to 4, so the cap of three cycles ended the loop, not the threshold of 8.

The fork: bull and bear prompts both branch from the reflected response with background set to true, and your code polls each job. Both quote the 35.2 P/E from the first research pass without calling the tool again.
