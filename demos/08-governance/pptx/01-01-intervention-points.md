---
slide: "01-01"
topic: 01-guardrails
title: "Intervention points in an agent run"
subtitle: "Where a guardrail control scans the content"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/01-01-intervention-points.svg
order: 3
source: demos/08-governance/01-guardrails/readme.md
gamma_prompt: >
  Slide titled "Intervention points in an agent run", subtitle "Where a guardrail
  control scans the content". Left half: five short bullets about what a control
  names, the four intervention points, the agent guardrail overriding the model
  guardrail, and the added latency. Right half: a wide diagram of one agent run from
  the user to the agent to a tool and back, with four amber checkpoints labelled User
  input, Tool call, Tool response and Output. Clean minimal style, white background,
  show the whole diagram without cropping.
---

# Intervention points in an agent run

## Content

- A control names a **risk**, an intervention point and an action
- **User input** (`Prompt`) and output (`Completion`) apply to models and agents
- **Tool call** (`PreToolCall`) and tool response (`PostToolCall`) apply to agents only, in preview
- The agent's guardrail in `rai_config` **fully overrides** the model's
- Each intervention point adds roughly **50 to 100 ms**

## Speaker Notes

A guardrail is a named set of controls that Microsoft Foundry applies at runtime. This slide places its four intervention points on the path of one agent run.

A control names a risk: it also names the intervention point where the content is scanned and the action to take. For agents the action is annotate and block.

User input and output: these two apply to models and agents alike. In the policy resource their `source` values are `Prompt` and `Completion`.

Tool call and tool response: these two apply to agents only and are in preview. They take effect only for tools that support moderation, such as Azure AI Search, OpenAPI and Bing Grounding.

Fully overrides: without its own guardrail an agent inherits the one of its model deployment, `Microsoft.DefaultV2` unless you changed it. With `rai_config` set, a control missing from the agent's guardrail does not run just because the model's guardrail has it.

50 to 100 ms: every intervention point adds roughly that much latency to the run.
