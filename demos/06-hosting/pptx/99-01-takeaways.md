---
slide: "99-01"
topic: 06-hosting
title: "Takeaways"
subtitle: "Pick where the agent runs, then how it is reached"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/06-hosting/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers in their late twenties, a man and a woman, at a light-oak high table in a
  bright lounge by a tall window: he holds a tablet towards her, she reads it, smiles and gives a thumbs up,
  the tablet screen showing only a soft blurred pastel sidebar shape. Both people centred in the frame, faces
  and hands fully visible, an armchair and a green plant behind them. Modern, light, bright and friendly:
  high-key natural daylight, white and light-oak contemporary interior, soft pastel accents in light blue,
  mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes and colours, never
  readable UI. 2:3 portrait, contemporary editorial photography. Avoid: readable text, logos, watermarks,
  dark or moody lighting, vintage or retro decor, grime, clutter.
order: 23
source: demos/06-hosting/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "Pick where the agent runs, then how it is reached". Side-accent
  layout: the five bullets on the left on plain white, the provided portrait photograph of two engineers at a
  high table with a tablet filling the right side, no text over the photo. Use exactly the one image given for
  this card; do not generate or add any other images, icons or logos.
---

# Takeaways

## Content

- **Foundry hosting**: a hosted agent runs your code under its own Entra identity, and a routine starts it on a timer, a schedule or a GitHub event
- **Durable state**: an orchestration waits for a reviewer without holding a process and survives a restart
- **Self-hosting**: the agent is a library in your web server, which owns routing, authentication and storage
- **A2A at scale**: a registry routes by capability and health, ETags guard shared state, and the tenant is part of every key
- **AG-UI**: one request, a stream of typed events, and approvals that arrive as interrupts

## Speaker Notes

Close with the decisions this module prepared: where the agent runs, and how agents and people reach it.

Foundry hosting: a hosted agent is your own code on Foundry-run compute with a dedicated Entra identity. A routine pairs one trigger with one action and keeps a run history.

Durable state: the Durable Task Scheduler holds the session and the orchestration, so the host can stop or restart while a reviewer thinks. The wait costs no compute.

Self-hosting: Agent Framework gives you helpers that turn a protocol request into an agent run. Middleware, authentication, sessions and scaling stay your decision.

A2A at scale: one card describes one agent, and a registry answers which agent may take the work right now. ETags stop lost updates, the tenant id is part of every key, and conflicts are settled by rank with an audit trail.

AG-UI: a client posts once and reads typed events until the run ends. A tool that needs approval ends the run with an interrupt, and the client resumes it with a decision.
