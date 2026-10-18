---
slide: "07-02"
topic: 07-responsible-ai
title: "The audit trail"
subtitle: "Every decision names its agent version, its input hash and its submission"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/07-02-audit-trail.svg
order: 18
source: demos/08-governance/07-responsible-ai/demo-responsible-ai.md
gamma_prompt: >
  Slide titled "The audit trail", subtitle "Every decision names its agent version, its input hash and
  its submission". Layout: Two columns. Left column: the bullets. Right column, wider than the left: the
  provided diagram of the agent version, the three audit entries of one submission and the audit report
  questions as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never
  cropped, never used as a background or side accent. Use exactly the one image given for this card; do
  not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# The audit trail

## Content

- One JSON line per review call in `out/audit.jsonl`
- `agent_id` names the pinned **agent version**, and its metadata carries the prompt label
- `input_hash` is a SHA-256 of the exact text; the log stores no code
- `correlation_id` groups the three reviews of one **submission**
- `audit_report.py` answers its questions from the log alone

## Speaker Notes

This slide shows what one submission leaves in the audit trail and how `audit_report.py` reads it back.

One line per call: every review appends a JSON line that answers who, what, when, with which version and on which input.

Agent version: the probe stores `prompt_version` as metadata on the Foundry version and builds `agent_id` from the name and the version number. Every call pins version 1 in its `agent_reference`, so each entry names the version that really answered.

Input hash: an auditor who gets the original text can recompute the SHA-256 and prove which input produced a decision. The three reviews of one sample carry different hashes because each had a different header.

Correlation id: one id per code sample groups its three reviews into a decision trail.

Report: `audit_report.py` groups by `agent_id`, `model_version` and `prompt_version` for the version timeline, by `output.team_context` for the scores and by `correlation_id` for one trail. It makes no call to Foundry.
