---
slide: "06-02"
topic: 06-foundry-toolbox
title: "Versions and promotion"
subtitle: "The consumer endpoint follows the default version"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/06-02-version-promotion.svg
order: 14
source: demos/02-mcp/06-foundry-toolbox/demo-foundry-toolbox.md
gamma_prompt: >
  Slide titled "Versions and promotion" with subtitle "The consumer endpoint follows the default
  version". Left half: four short bullets about immutable versions, testing on the version endpoint,
  promoting with update and an agent that stays unchanged. Right half: the provided diagram of the
  consumer endpoint moving from version 1 to version 2 while a test client reads the version endpoint,
  at full legibility on white. Use exactly the one image given for this card; do not generate or add
  any other images, icons or logos.
---

# Versions and promotion

## Content

- Every `create_version` adds an **immutable version** and leaves the default alone
- Test a new version first on its **version endpoint**, `/versions/2/mcp`
- `update(default_version="2")` **promotes** it: the consumer endpoint now serves version 2
- The agent stays as it is; roll back with `update` and the earlier version

## Speaker Notes

This slide is Step 4 of the lab: release version 2 of the toolbox without touching the agent. Version 2 keeps only the snippets tools.

Immutable versions: `promote.py` creates version 2, and its output says the default is still 1. Creating a version does not change what the consumer endpoint serves.

Test first: `list_tools.py 2` reads the version endpoint and lists three tools, while the consumer endpoint still lists six. The version endpoint is there for testing before promotion.

Promotion: `promote.py publish 2` calls `update` with `default_version="2"`, and the consumer endpoint now lists three tools. The .NET host started afterwards discovers 3 tools.

No redeployment: `agent_toolbox.py` would now see only the snippets tools, and nothing in it changed. To roll back, call `update` with the earlier version.
