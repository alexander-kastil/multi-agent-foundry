---
slide: "06-03"
topic: 06-agent-lifecycle
title: "Retiring an agent version"
subtitle: "Deprecate, warn, block, archive, repoint, delete"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/06-03-retirement-sequence.svg
order: 16
source: demos/08-governance/06-agent-lifecycle/demo-agent-lifecycle.md
gamma_prompt: >
  Slide titled "Retiring an agent version", subtitle "Deprecate, warn, block, archive, repoint, delete".
  Layout: Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram
  of the six retirement steps in two rows, the deprecation window and the deletion date, as an inline
  content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as
  a background or side accent. Use exactly the one image given for this card; do not generate or add any
  other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# Retiring an agent version

## Content

- At least **90 days** between deprecation and deletion
- A tenant with a pin keeps working and sees a **deprecation warning**
- A tenant without a pin gets **HTTP 410** with the replacement
- Archive the manifest, **repoint** the stable endpoint, then delete the version

## Speaker Notes

This slide follows `retirement.py` as it retires Security Analyzer 2.2.0 in favour of 2.3.0. The script plays the deletion day on a fixed date, so the output is reproducible.

Notice period: 2.2.0 was deprecated on 2026-09-01 and is deleted on 2026-12-01, 91 days later. The code enforces a minimum of 90 days.

Warn: standard-northwind holds a pin on 2.2.0 until 2026-10-30, so it receives HTTP 200 with a `deprecation_warning` that names the migration target and the days left.

Block: standard-contoso has no pin and receives HTTP 410 `agent_deprecated` with the replacement. The deletion date falls after the latest pin expiry, so no pin blocks the deletion.

Archive, repoint, delete: the manifest goes to `out/`, and the stable endpoint moves to version 2 before `delete_version` runs. The endpoint therefore never points at a version that no longer exists.
