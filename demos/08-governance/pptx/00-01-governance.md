---
slide: "00-01"
topic: 08-governance
title: "Security, Governance & Delivery"
subtitle: "Controls around Microsoft Foundry agents, one layer at a time"
layout: title-only
visual-weight: bg
visual-type: photo
media-source: nano-banana
media-file: demos/08-governance/pptx/images/00-01-governance.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers in their late twenties, a woman and a man in modern work-casual
  clothes, stand side by side at a tall light-oak desk in a bright glass-walled team room, reviewing a
  release together: she points at a large wall display that shows a softly blurred row of pipeline
  blocks, most of them mint green and one soft amber, while he holds an open laptop turned towards her
  and nods. Both people are centred in the vertical frame, seen from the waist up, with window light
  behind them. Modern, light, bright and friendly: high-key natural daylight, white and light-oak
  contemporary interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s
  design. Screens show only soft blurred shapes and colours, never readable UI. 2:3 portrait,
  contemporary editorial photography. Avoid: readable text, logos, watermarks, dark or moody lighting,
  vintage or retro decor, grime, clutter, extra people, padlocks, shields or other security symbols.
order: 1
source: demos/08-governance/readme.md
gamma_prompt: >
  Title slide "Security, Governance & Delivery" with subtitle "Controls around Microsoft Foundry agents,
  one layer at a time". Side-accent layout: title, subtitle and three short bullets on the left on plain
  white, the provided portrait photograph of two engineers reviewing a release at a wall display filling
  the right side, no text over the photo. Use exactly the one image given for this card; do not generate
  or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# Security, Governance & Delivery

## Content

- **Untrusted text**: guardrails at the platform and defenses in the prompt
- **Credentials**: an identity for every agent, isolated tenants and secrets
- **Change**: quality gates, a lifecycle for versions, fairness probes and an audit trail

## Speaker Notes

This slide opens the module that puts production controls around Microsoft Foundry agents. An agent that reaches production holds credentials, reads untrusted text and changes behavior with every prompt edit, so it needs the same controls as any other service.

Untrusted text: guardrails scan what goes into and out of an agent at the platform. Layered defenses in the prompt then handle the attacks that get past them.

Credentials: every agent gets its own Microsoft Entra identity. Tenants and secrets are isolated so that one agent reaches only its own data and keys.

Change: quality gates in the pipeline, a lifecycle for versions, fairness probes and an audit trail govern every change. Every topic ships a runnable Python project on the Responses API that creates its agents under `m8-` names and deletes them at the end.
