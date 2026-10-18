---
slide: "99-01"
topic: 05-orchestration
title: "Takeaways"
subtitle: "What to take into the hosting module"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/05-orchestration/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Three software engineers, two women and a man in their late twenties to early thirties, stand
  at a high light-oak table beside a large window in a bright modern team space after a successful run:
  one woman turns her laptop so the others can see it, the man nods, and the second woman smiles with a
  coffee cup in her hand. The three people are in the centre of the frame, green plants and daylight
  behind them. The laptop screen stays blank of any text, and the cup and the table carry no text or logo.
  Modern, light, bright and friendly: high-key natural daylight, white and light-oak contemporary
  interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s design. Screens show
  only soft blurred shapes and colours, never readable UI. 2:3 portrait, people centered in the frame,
  contemporary editorial photography. Avoid: readable text, logos, watermarks, extra people, dark or moody
  lighting, vintage or retro decor, grime, clutter.
order: 19
source: demos/05-orchestration/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What to take into the hosting module". Content card: five
  short bullets on the left on plain white, the provided photograph of three engineers at a high table in
  a bright team space as a side accent on the right, no text over the photo. Use exactly the one image
  given for this card; do not generate or add any other images, icons or logos.
---

# Takeaways

## Content

- Model fixed steps as a typed graph; **supersteps** make fan-out and fan-in predictable
- **Checkpoints** at superstep boundaries let a run wait for a person and resume in another process
- Pick the builder by control flow, and bound every manager loop with round and stall limits
- Return a **status** from every spoke call, and decide in advance how many answers are enough
- Export portal workflows as YAML before December 1, 2026 and run them with `WorkflowFactory`

## Speaker Notes

Five points to take into the hosting module, where these workflows run as agents.

Graphs: executors and edges make the structure explicit. A fan-out of three one-second checks costs one second, because all targets of a superstep run together.

Checkpoints: a checkpoint is written at every superstep boundary, so a request for a human decision can wait for hours. Treat the checkpoint directory like a credential store and list custom request types in `allowed_checkpoint_types`.

Builders: sequential, concurrent and handoff fix the control flow in advance, while group chat and Magentic let a manager pick the next speaker. Termination conditions, round limits and stall counts stop a confused team from spending tokens.

Resilience: a spoke call returns `success`, `timeout`, `error` or `circuit_open` instead of raising, and a quorum decides how many parallel answers are enough. A supervisor loop with `max_retries` ends with `approved: False` rather than looping forever.

Migration: Foundry retires portal workflows on December 1, 2026, and the exported YAML runs in Agent Framework with `WorkflowFactory`. Run both with the same test requests and keep the original until the replacement is confirmed.
