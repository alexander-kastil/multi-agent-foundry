---
slide: "00-02"
topic: 08-governance
title: "Overview"
subtitle: "Seven layers of control around a Foundry agent"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/00-02-overview.svg
order: 2
source: demos/08-governance/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "Seven layers of control around a Foundry agent". Layout: Two
  columns. Left column: the bullets. Right column, wider than the left: the provided diagram of the seven
  numbered topic cards in three rows as an inline content image at its natural 16:9 aspect ratio, scaled
  to fit the column, never cropped, never used as a background or side accent. Use exactly the one image
  given for this card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# Overview

## Content

- **Untrusted text**: Prompt Shields at four intervention points, then layered defenses in the prompt
- **Credentials**: an Entra identity per agent, and tenant data reached only through a verified token
- **Release**: quality gates in GitHub Actions that block a bad agent version
- **Operation**: version manifests, the version selector, token limits per tenant and retirement with notice
- **Accountability**: fairness probes and an audit trail per agent version

## Speaker Notes

After the title, this slide walks the seven topics in the order the module runs them. They group around the three facts from the module intro: an agent in production reads untrusted text, holds credentials and changes behavior with every prompt edit.

Untrusted text: guardrails apply controls at user input, tool call, tool response and output, with Prompt Shields for prompt and document attacks. The prompt injection topic stacks delimiters, a hardened prompt and input and output checks on top and measures them against an attack corpus.

Credentials: Entra Agent ID gives every agent its own identity and passes a user's permissions to MCP tools through OAuth. Zero Trust derives the tenant from a verified token, rotates keys without downtime and gives each agent only its own secrets.

Release: the CI/CD topic turns each release risk into a script that exits non-zero. GitHub Actions turns that exit code into a blocked pull request or a refused release.

Operation: the lifecycle topic approves versions with a manifest, rolls them out in tiers and moves the stable endpoint with the version selector. A token gateway meters every tenant, and old versions retire with at least 90 days notice.

Accountability: the responsible AI topic probes a reviewer agent for unequal treatment. Its audit trail names the agent version behind every decision.
