---
slide: "99-01"
topic: 07-evaluation
title: "Takeaways"
subtitle: "What to take into the next module"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/07-evaluation/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Three software engineers, women and men in their late twenties to mid thirties in modern
  work-casual clothes, stand at a large whiteboard in a bright meeting room arranging blank pastel
  sticky notes into one long row; one places a mint note at the end of the row while the other two
  smile and point at it. The group centred in the frame, the whiteboard and the sticky notes blank.
  Modern, light, bright and friendly: high-key natural daylight, white and light-oak contemporary
  interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s design. Screens
  show only soft blurred shapes and colours, never readable UI. 2:3 portrait, contemporary editorial
  photography. Avoid: readable text, writing on the sticky notes or the whiteboard, logos, watermarks,
  extra people, dark or moody lighting, vintage or retro decor, grime, clutter.
order: 20
source: demos/07-evaluation/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What to take into the next module". Side-accent layout:
  five short bullets on the left on plain white, the provided portrait photograph of three engineers
  arranging sticky notes at a whiteboard on the right side, no text over the photo. Use exactly the one
  image given for this card; do not generate or add any other images, icons or logos.
---

# Takeaways

## Content

- **Trace** every step so a slow hop or a wrong tool argument shows up
- Score runs with **evaluators**, gate releases on a baseline and keep scoring live traffic
- The **optimizer** rewards only what your dataset and evaluators measure
- Give every cost lever a **quality floor**
- **Replay** a captured failure and bisect versions before you change production

## Speaker Notes

Close with the habits the module was built around, from the first trace to the postmortem.

Trace: each step is a span and one trace ID ties a request together across services, so a slow hop or a wrong tool argument is visible.

Score and gate: agent evaluators score what the agent did, a suite with a baseline blocks a regressed change, and continuous evaluation keeps scoring after release.

Optimizer: the agent optimizer replaces guessing with measurement, but it can only reward what the dataset and evaluators measure. Confirm every promoted version with your own evaluation.

Quality floor: routing, caching and token budgets each save tokens or waiting time, and each needs a guard so that savings never turn into wrong answers.

Replay: a snapshot with the prompt hash and masked tool data reproduces a failure without production calls, and bisection names the version that broke it. The lasting fix goes upstream: output validation, canary releases and a regression probe.
