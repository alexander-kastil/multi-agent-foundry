---
slide: "00-01"
topic: 06-hosting
title: "Hosting, Durable Agents & Protocols"
subtitle: "Where an agent runs, what wakes it and how others reach it"
layout: title-only
visual-weight: bg
visual-type: photo
media-source: nano-banana
media-file: demos/06-hosting/pptx/images/00-01-hosting-protocols.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers in their early thirties, a woman and a man, in a bright operations corner
  of an engineering studio: she holds an open laptop on her forearm and looks up at a large wall-mounted
  display, he stands beside her and points at one tile in a grid of softly blurred connected pastel tiles on
  that display, both smiling. Both people centred in the frame, upper bodies fully visible, a light-oak
  sideboard and a tall green plant behind them. Modern, light, bright and friendly: high-key natural
  daylight, white and light-oak contemporary interior, soft pastel accents in light blue, mint and lavender,
  green plants, 2020s design. Screens show only soft blurred shapes and colours, never readable UI. 2:3
  portrait, contemporary editorial photography. Avoid: readable text, logos, watermarks, dark or moody
  lighting, vintage or retro decor, grime, clutter.
order: 1
source: demos/06-hosting/readme.md
gamma_prompt: >
  Title slide "Hosting, Durable Agents & Protocols" with subtitle "Where an agent runs, what wakes it and how
  others reach it". Side-accent layout: title, subtitle and bullets on the left half on plain white, the
  provided portrait photograph of two engineers at a wall display filling the right side, no text over the
  photo. Use exactly the one image given for this card; do not generate or add any other images, icons or
  logos.
---

# Hosting, Durable Agents & Protocols

## Content

- **Foundry runs it**: hosted agents with their own Entra identity, routines and the `azd ai agent` loop
- **You run it**: durable agents on Azure Functions and self-hosted endpoints in ASP.NET Core and FastAPI
- **Others reach it**: other agents over **A2A**, people at a screen over **AG-UI**

## Speaker Notes

An agent that works in a notebook is not yet a service. This module answers where it runs, which identity it runs as, what wakes it up, how it survives a restart and how other agents and web front ends talk to it.

Foundry runs it: hosted agents get a dedicated Entra identity, routines start an agent on a timer, a schedule or a GitHub event, and `azd ai agent` ships and monitors them.

You run it: durable agents on Azure Functions and the Durable Task Scheduler keep their state outside the process. Self-hosted endpoints in ASP.NET Core and FastAPI put the agent inside a server you already operate.

Others reach it: A2A connects agents to each other, from one agent card to a whole network. AG-UI and CopilotKit connect an agent to a person in the browser.
