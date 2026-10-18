---
slide: "00-01"
topic: 01-foundry-essentials
title: "Foundry essentials for agent developers"
subtitle: "The project, model, SDK and tooling every later module builds on"
layout: title-only
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/01-foundry-essentials/pptx/images/00-01-foundry-essentials.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen,
  no diptych, no collage. Two software engineers, a woman and a man in their late
  twenties to early thirties in modern work-casual clothes, at a light-oak desk by
  floor-to-ceiling windows. She types on an open laptop whose screen shows softly blurred
  lines of a terminal; he leans in beside her and points at a second monitor where a
  softly blurred chat window shows an answer appearing line by line, and both smile. The
  two people are centred in the frame, seen from the waist up, in a vertical composition.
  Modern, light, bright and friendly: high-key natural daylight, white and light-oak
  contemporary interior, soft pastel accents in light blue, mint and lavender, green
  plants, 2020s design. Screens show only soft blurred shapes and colours, never readable
  UI. 2:3 portrait, contemporary editorial photography. Avoid: readable text, logos,
  watermarks, extra people, dark or moody lighting, vintage or retro decor, grime,
  clutter.
order: 1
source: demos/01-foundry-essentials/readme.md
gamma_prompt: >
  Title slide "Foundry essentials for agent developers" with subtitle "The project, model,
  SDK and tooling every later module builds on". Side-accent layout: title, subtitle and
  three bullets on the left on plain white, the provided portrait photograph of two
  engineers watching an answer appear on a monitor filling the right side, no text over
  the photo. Use exactly the one image given for this card; do not generate or add any
  other images, icons or logos.
---

# Foundry essentials for agent developers

## Content

- **Project and model**: one Foundry project endpoint, model deployments and model router
- **Code**: the Foundry SDK in Python and C#, and the Responses API with conversations and streaming
- **Operations**: Foundry Toolkit, `azd ai`, the Foundry MCP server and the context you send with every call

## Speaker Notes

Every later module in this class builds on the same few Microsoft Foundry basics, and this module covers exactly those, so the agent modules can start with agent code instead of setup questions. You work on Foundry (new) throughout.

Project and model: one project with one endpoint is where every agent runs, and model router lets Foundry choose the underlying model for each call. The renamed Foundry roles decide who may do what.

Code: the Foundry SDK reaches the project in Python and C#, and in .NET Microsoft.Extensions.AI adds the provider-neutral `IChatClient`. The Responses API then carries a conversation and streams the answer.

Operations: the Foundry Toolkit, the `azd ai` extensions and the Foundry MCP server let you operate all of it quickly. Context engineering closes the module by deciding what goes into every call.
