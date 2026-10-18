---
slide: "00-01"
topic: 04-agent-framework
title: "Agent Framework: Agents, Tools & Skills"
subtitle: "Building the agent layer one concern at a time"
layout: title-only
visual-weight: bg
visual-type: photo
media-source: nano-banana
media-file: demos/04-agent-framework/pptx/images/00-01-agent-framework.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers, a woman in her early thirties and a man in his late twenties, stand at a
  large whiteboard wall in a bright modern engineering studio: she presses a blank pastel sticky note onto a
  neat vertical column of blank sticky notes while he holds an open laptop and points at the column, both
  focused and smiling. Both people are in the centre of the frame, a tall green plant beside the whiteboard.
  The sticky notes, the whiteboard and the laptop screen stay blank of any text. Modern, light, bright and
  friendly: high-key natural daylight, white and light-oak contemporary interior, soft pastel accents in light
  blue, mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes and colours, never
  readable UI. 2:3 portrait, people centred in the frame, contemporary editorial photography. Avoid: readable
  text, logos, watermarks, extra people, dark or moody lighting, vintage or retro decor, grime, clutter.
order: 1
source: demos/04-agent-framework/readme.md
gamma_prompt: >
  Title slide "Agent Framework: Agents, Tools & Skills" with subtitle "Building the agent layer one concern at a
  time". Side-accent layout: title, subtitle and three bullets on the left half on plain white, the provided
  photograph of two engineers stacking sticky notes at a whiteboard filling the right half, no text over the
  photo. Use exactly the one image given for this card; do not generate or add any other images, icons or logos.
---

# Agent Framework: Agents, Tools & Skills

## Content

- One **Agent** class owns instructions, tools, sessions and middleware; the chat client decides where the model runs
- **Sessions, context providers and middleware** carry state, memory and guardrails around every run
- **Tools, skills and the harness agent** let it act, follow procedures and run long tasks

## Speaker Notes

Microsoft Agent Framework is the open-source SDK for building agents in Python and C#, generally available since April 2026. This module builds the agent layer one concern at a time, from the client up to a harness agent.

One Agent class: the agent owns instructions, tools, sessions and middleware. The chat client decides whether the model runs in a Foundry deployment, an agent Foundry owns, OpenAI, Azure OpenAI or Claude.

State, memory and guardrails: sessions carry a conversation between runs, context providers build each request, and middleware wraps the agent from the outside.

Acting and long tasks: tools and approvals let the agent act, skills load company procedures on demand, and the harness agent plans, acts and compacts its own context.
