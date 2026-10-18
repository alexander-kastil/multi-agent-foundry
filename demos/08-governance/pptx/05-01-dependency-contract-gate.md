---
slide: "05-01"
topic: 05-cicd-gates
title: "The dependency and contract gate"
subtitle: "Deployment waves from the manifest, breaking changes with their dependents"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/05-01-dependency-contract-gate.svg
order: 12
source: demos/08-governance/05-cicd-gates/demo-cicd-gates.md
gamma_prompt: >
  Slide titled "The dependency and contract gate", subtitle "Deployment waves from the manifest,
  breaking changes with their dependents". Layout: Two columns. Left column: the bullets. Right column,
  wider than the left: the provided diagram of seven agents in five deployment waves with the contract
  gate result as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column,
  never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# The dependency and contract gate

## Content

- The manifest lists each agent with its `depends_on`; `deployment_waves` turns it into **waves** that deploy in parallel
- The same graph writes the deploy workflow, so `needs:` always matches the manifest
- A removed tool, a new required parameter or a removed response field is **breaking**; added optional fields are not
- Each breaking change names its **dependents**, and the gate exits with code 1

## Speaker Notes

This slide shows the first gate of the topic: one script, `validate_compatibility.py`, orders the deployment and checks the tool contracts before anything reaches Foundry.

Deployment waves: `deployment_waves` repeatedly takes every agent whose dependencies are already done. The seven code review agents land in five waves, and the agents in one wave deploy in parallel.

Generated workflow: the same graph writes `out/deploy-agents.yml`, so the `orchestrator` job needs exactly the four agents its manifest line names. The `needs:` lists cannot drift from the manifest.

Breaking changes: the gate compares `contracts/current.json` with the baseline. In the lab the style checker turns the optional `style_guide` parameter into a required one, while the security scanner only adds an optional parameter and a response field, which the gate ignores.

Dependents and exit code: walking the graph names `orchestrator` and `reporting` as affected dependents. The script prints `Gate result: FAIL` and exits with code 1, and a required status check turns that into a blocked merge.
