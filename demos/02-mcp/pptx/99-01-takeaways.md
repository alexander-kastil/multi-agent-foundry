---
slide: "99-01"
topic: 02-mcp
title: "Takeaways"
subtitle: "What makes a tool server ready for agents"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/02-mcp/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no collage. A small engineering team of four adults, women and men in their late twenties to mid thirties, modern work-casual, sits around a light wooden table in a bright glass-walled meeting room. One engineer turns her laptop towards a colleague, who nods, while the other two look at a large wall screen that shows softly blurred rows of green status tiles; the mood is calm and pleased. The laptop screen, the wall screen and the notebooks on the table show no readable text, badges or logos. Modern, light, bright and friendly: high-key natural daylight, white and light-oak contemporary interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes and colours, never readable UI. 2:3 portrait, people centred in the frame, contemporary editorial photography. Avoid: readable text, logos, watermarks, extra people, dark or moody lighting, vintage or retro decor, grime, clutter.
order: 15
source: demos/02-mcp/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways" with subtitle "What makes a tool server ready for agents" and five
  short bullets. Text on the left half on plain white, the provided photograph of an engineering team
  in a bright glass meeting room as a side accent on the right half, no text over it. Use exactly the
  one image given for this card; do not generate or add any other images, icons or logos.
---

# Takeaways

## Content

- Give each tool **one job**, a verb-and-noun name and described parameters
- Use **Streamable HTTP** for any server a Foundry agent must reach
- Accept only tokens issued for your server, and keep keys in a **project connection**
- Return a **structured fallback** when the backend fails, and tell the agent what it means
- Publish shared tools once in a **toolbox** and promote new versions

## Speaker Notes

This closing slide turns the module into five habits for a tool server that agents depend on.

One job per tool: keep the set small, name tools with a verb and a noun, and describe every parameter. A model chooses worse as the list gets longer.

Streamable HTTP for agents: Foundry Agent Service accepts only a remote MCP endpoint. A stdio server built for a local IDE has to be hosted over HTTP first.

Tokens and keys: a server must accept only tokens issued for it, and a key an agent needs lives in a project connection, never in its code. Prefer Microsoft Entra authentication for production.

Structured fallback: when the backend fails, the tool returns a status and a fallback, and the agent instructions say what the agent may conclude. The tool and the instructions have to agree, or the model fills the gap.

Publish once: a toolbox gives every agent one endpoint, and promoting a new default version changes the tools without a redeployment.
