---
slide: "99-01"
topic: 08-governance
title: "Takeaways"
subtitle: "What each layer leaves in your project"
layout: content
visual-weight: 1/2
visual-type: photo
media-source: nano-banana
media-file: demos/08-governance/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. A team of four software engineers, two women and two men in their late twenties and early
  thirties in modern work-casual clothes, gathered around a low light-oak table in a bright lounge corner
  with sofas and large plants at the end of a release day: one of them turns an open laptop towards the
  others, its screen a softly blurred column of mint green check shapes, and the group smiles and leans
  in, one raising a coffee cup. The group is centred in the vertical frame with bright window light
  behind. Modern, light, bright and friendly: high-key natural daylight, white and light-oak
  contemporary interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s
  design. Screens show only soft blurred shapes and colours, never readable UI. 2:3 portrait,
  contemporary editorial photography. Avoid: readable text, logos, watermarks, dark or moody lighting,
  vintage or retro decor, grime, clutter, extra people, the back of the laptop.
order: 19
source: demos/08-governance/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What each layer leaves in your project". Side-accent
  layout: title, subtitle and five bullets on the left on plain white, the provided portrait photograph
  of a team smiling around a laptop in a bright lounge filling the right side, no text over it. Use
  exactly the one image given for this card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# Takeaways

## Content

- Attach a **guardrail** to the agent, and rerun the attack corpus after every prompt change
- Give every agent its own **identity**, and read the tenant only from a verified token
- Turn each release risk into a **gate** that exits non-zero
- Move traffic with the **version selector**, and retire versions with at least 90 days notice
- Probe for unequal treatment, and log the **agent version** behind every decision

## Speaker Notes

Close with the habit each topic leaves in the project, in the order the module built them.

Guardrails and prompt defenses: a guardrail scans input, tool calls, tool responses and output at the platform. Prompt-level layers lower the odds of an attack but never remove it, so rerun the corpus whenever the prompt, the model or the tools change.

Identity and isolation: the agent identity holds the role on the target resource, and the on-behalf-of flow limits the agent to what the signed-in user may reach. The tenant comes from verified token claims, never from the model or a request field.

Gates: a gate prints its evidence and exits 0 or 1, and a required status check turns that exit code into a blocked merge. Test the failing path as carefully as the passing one.

Versions: pin production before a candidate exists, promote by moving the pin, and repoint the endpoint before a version is deleted. Tenants with a pin keep working until it expires.

Fairness and audit: compare the same input with and without the attribute that must not matter. Every audit line names the agent version, the prompt label and a hash of the exact input.
