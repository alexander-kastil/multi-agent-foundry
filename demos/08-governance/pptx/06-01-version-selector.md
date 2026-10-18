---
slide: "06-01"
topic: 06-agent-lifecycle
title: "The version selector on the stable endpoint"
subtitle: "Pin, promote and roll back without changing the URL"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/06-01-version-selector.svg
order: 14
source: demos/08-governance/06-agent-lifecycle/demo-agent-lifecycle.md
gamma_prompt: >
  Slide titled "The version selector on the stable endpoint", subtitle "Pin, promote and roll back
  without changing the URL". Layout: Two columns. Left column: the bullets. Right column, wider than the
  left: the provided diagram of four selector states, each showing which agent version the stable
  endpoint serves, as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for
  this card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# The version selector on the stable endpoint

## Content

- Every change creates an immutable **agent version**; the endpoint URL stays the same
- The default rule `@latest` serves a new version the moment it exists
- **Pinning** sends 100 percent of the traffic to one named version
- A call that names a version in its `agent_reference` bypasses the selector
- Promotion and rollback are the same call with a different version number

## Speaker Notes

This slide walks through the four states that `version_selector.py` prints, from the first version to the rollback.

Versions and endpoint: a Foundry agent version is an immutable snapshot, so even one prompt edit creates a new version. The stable endpoint URL never changes while versions come and go.

The default rule: `@latest` serves a new version as soon as it exists. That is why the script pins version 1 before it creates version 2.

Pinning: a fixed-ratio rule sends 100 percent of the traffic to the version you name. Traffic splitting between versions is not supported, so the endpoint always serves exactly one version.

Testing by reference: in the second state the endpoint still answers in the version 1 style. A call that names version 2 in its `agent_reference` already gets the `severity | CWE id | explanation` format.

Promote and roll back: both are the same update with a different version number. The selector moves from 1 to 2 and back to 1 without touching the versions.
