---
slide: "01-01"
topic: 01-prompt-agents
title: "Agent versions and references"
subtitle: "Pin a caller to a version or follow the latest one"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/01-01-version-references.svg
order: 3
source: demos/03-agent-service/01-prompt-agents/readme.md
gamma_prompt: >
  Slide titled "Agent versions and references", subtitle "Pin a caller to a version or
  follow the latest one". Left half: four short bullets on create_version adding the next
  version, immutable versions, pinning a caller with a name and a version, and a reference
  without a version that targets the latest one. Right half: the supplied diagram of three
  callers reaching version 1 or version 2 of m3-support-agent, shown whole without cropping.
  Clean minimal style, white background.
---

# Agent versions and references

## Content

- Calling `create_version` with an existing name adds the **next version** and never overwrites
- Versions are **immutable**, so a version number is a stable contract
- A caller that pins `m3-support-agent:1` keeps **version 1** while you test version 2
- A reference without a version targets the **latest version**

## Speaker Notes

This slide shows how callers find an agent once it has more than one version. In the demo one name, m3-support-agent, gets two versions with different instructions.

Next version: calling create_version again with the same name creates version 2 instead of replacing version 1. The demo prints Created m3-support-agent:1 and later Created m3-support-agent:2.

Immutable versions: a version cannot change after it is created, so its number is a contract. In Step 5 version 1 still answers in one short sentence after version 2 exists.

Pinning: the agent travels in extra_body as an agent_reference with a name and a version. It goes there because the OpenAI Responses API has no agent field of its own.

Latest version: a reference that names no version reaches the newest one. Step 5 sends the same question to both versions so you can compare the answers side by side.
