---
slide: "00-02"
topic: 06-hosting
title: "Overview"
subtitle: "From hosting that Foundry runs to the protocols between agents and people"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/00-02-overview.svg
order: 2
source: demos/06-hosting/readme.md
gamma_prompt: >
  Slide titled "Overview" with subtitle "From hosting that Foundry runs to the protocols between agents and
  people". Four short bullets walk the path through the module: Foundry-run hosting, your own hosting, agent
  to agent, agent to person. Beside them, the provided diagram of eight numbered topic cards in three
  columns, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit, never cropped,
  never used as a background or side accent. Use exactly the one image given for this card; do not generate
  or add any other images, icons or logos.
---

# Overview

## Content

- **Foundry runs it**: hosted agents, routines on a timer, schedule or GitHub event, and the `azd ai agent` loop
- **You run the host**: durable agents on Azure Functions with the Durable Task Scheduler, and self-hosted Responses endpoints
- **Agent to agent**: A2A agent cards and endpoints, then a registry, shared state and conflict rules for a network
- **Agent to person**: AG-UI endpoints for agents and workflows, with CopilotKit in the browser

## Speaker Notes

This slide walks the path through the module once, so every later slide has a place on it. Eight topics fall into three groups, and every topic has a Python project; topics 1 to 8 also have a C# one.

Foundry runs it: topics 1 to 3 cover hosted agents with their own identity, routines that pair one trigger with one action, and the `azd ai agent` commands that scaffold, run, deploy and monitor them.

You run the host: topic 4 moves agent state into the Durable Task Scheduler, so an approval wait costs no compute. Topic 5 maps an agent onto a Responses endpoint in ASP.NET Core or FastAPI.

Agent to agent: topic 6 teaches agent cards and A2A endpoints. Topic 7 adds what a network of agents needs: capability routing, ETag-protected shared state, tenant isolation and conflict rules.

Agent to person: topic 8 serves agents and workflows over AG-UI, with approvals as interrupts and a CopilotKit runtime in front of the browser.
