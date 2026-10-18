---
slide: "99-01"
topic: 03-agent-service
title: "Takeaways"
subtitle: "What to take into the next module"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/03-agent-service/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Three software engineers in their late twenties and early thirties, two women and one man, around
  a light-oak shared desk by a tall window at the end of a workshop day: one turns her laptop towards the
  other two, who lean in, nod and smile, the laptop screen showing only soft blurred pastel shapes. All three
  people centred in the frame, faces and hands fully visible, bright window light behind them. Modern, light,
  bright and friendly: high-key natural daylight, white and light-oak contemporary interior, soft pastel
  accents in light blue, mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes
  and colours, never readable UI. 2:3 portrait, contemporary editorial photography. Avoid: readable text,
  logos, watermarks, dark or moody lighting, vintage or retro decor, grime, clutter.
order: 23
source: demos/03-agent-service/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What to take into the next module". Side-accent layout: the
  five bullets on the left on plain white, the provided portrait photograph of three engineers around a laptop
  filling the right side, no text over the photo. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Takeaways

## Content

- Publish **agent versions** and pin calls with `name:version`
- Attach built-in tools instead of hosting a tool server
- Ground answers in **knowledge bases** and measured hybrid retrieval
- Carry context with memory stores and stored conversation ids
- Put **voice** in front of an agent with Voice Live or a voice-based prompt agent

## Speaker Notes

Close with the habits this module built, from the first agent version to an agent that speaks.

Versions: versions are immutable, so a caller that pins name:version keeps the behavior it was tested against while you publish the next one.

Built-in tools: Code Interpreter, File Search and Web Search run inside Foundry Agent Service, so there is no tool server to host and no function loop to write.

Knowledge: a Foundry IQ knowledge base serves many agents over MCP, and every hybrid search and reranking choice is measured on labeled queries.

State and voice: memory stores and stored conversation ids let an agent pick up where it stopped. Voice Live puts speech in front of an existing text agent, and a voice-based prompt agent runs the whole speech pipeline itself.
