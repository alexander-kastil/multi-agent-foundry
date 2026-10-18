---
slide: "00-01"
topic: 07-evaluation
title: "Evaluation, Observability & Optimization"
subtitle: "From traces to incident response"
layout: title-only
visual-weight: 1/2
visual-type: photo
media-source: nano-banana
media-file: demos/07-evaluation/pptx/images/00-01-evaluation-observability.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers, a woman and a man in their late twenties to mid thirties in modern
  work-casual clothes, stand at a tall light-oak standing desk in front of a large wall monitor that
  shows softly blurred trace timelines and bar charts in pastel colours; she points at one tall bar on
  the chart while he holds an open laptop and nods. Both people centred in the frame, seen from the side
  at eye level, the monitor behind them. Modern, light, bright and friendly: high-key natural daylight,
  white and light-oak contemporary interior, soft pastel accents in light blue, mint and lavender, green
  plants, 2020s design. Screens show only soft blurred shapes and colours, never readable UI. 2:3
  portrait, contemporary editorial photography. Avoid: readable text, logos, watermarks, extra people,
  dark or moody lighting, vintage or retro decor, grime, clutter.
order: 1
source: demos/07-evaluation/readme.md
gamma_prompt: >
  Title slide "Evaluation, Observability & Optimization" with subtitle "From traces to incident
  response". Side-accent layout: title, subtitle and three bullets on the left on plain white, the
  provided portrait photograph of two engineers at a wall monitor filling the right side, no text over
  the photo. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Evaluation, Observability & Optimization

## Content

- **Observe**: trace every agent step with OpenTelemetry and Application Insights
- **Score**: evaluators, judged suites, baseline gates and live evaluation
- **Improve**: optimizer tuning, cost levers and incident replay

## Speaker Notes

This is the opening slide of the module: an agent that works in a demo can still fail in production in ways no one sees, and the module covers the loop that closes that gap.

Observe: traces show what the agent did, step by step, with OpenTelemetry spans exported to Application Insights.

Score: evaluators score each run, a suite with a baseline gates releases, and continuous evaluation and red teaming keep scoring after release.

Improve: the Foundry agent optimizer proposes better instructions, routing and caching cut cost, and incident response replays a failure when something still breaks. Several of these features are in preview, which each topic readme states.
