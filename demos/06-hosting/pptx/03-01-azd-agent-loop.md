---
slide: "03-01"
topic: 03-azd-ai-agent
title: "The azd ai agent loop"
subtitle: "From scaffold to a monitored hosted agent"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/03-01-azd-agent-loop.svg
order: 8
source: demos/06-hosting/03-azd-ai-agent/demo-azd-ai-agent.md
gamma_prompt: >
  Slide titled "The azd ai agent loop", subtitle "From scaffold to a monitored hosted agent". The left half holds
  four short bullets on the Azure Developer CLI agent commands: init writes azure.yaml against the existing project
  and model deployment; run starts the host on port 8088 and invoke --local calls it; azd deploy creates a version
  and waits until it is active; invoke, monitor and doctor operate the hosted agent. The right half shows the
  supplied diagram unchanged: a blue band for your machine with init, run and invoke --local, a purple band for the
  Foundry project with azd deploy, invoke and monitor, a loop arrow back to run, and a tall doctor box with its
  check groups and result. White background, clean sans-serif type, no extra imagery.
---

# The azd ai agent loop

## Content

- `init` writes **azure.yaml** against the existing project and model deployment
- `run` starts the host on **port 8088**, and `invoke --local` calls it
- `azd deploy` creates a **version** and waits until it is active
- `invoke`, `monitor` and `doctor` **operate** the hosted agent

## Speaker Notes

The `azd ai agent` commands turn a hand-made deployment into one loop that works the same in a terminal or a pipeline. The demo walks it in six steps.

azure.yaml: `init` points at the existing Foundry project with `--project-id` and reuses the `gpt-5.4` deployment with `--model-deployment`. Because `azd` searches parent folders for `azure.yaml`, the demo copies the starter out of the repository first.

Port 8088: `run` creates the virtual environment, installs the requirements and starts the host on port 8088, with Agent Inspector on 8087 unless you pass `--no-client`. `invoke --local` sends a message to that local host.

Version: the project already exists, so `azd deploy` is enough; it packages a ZIP, creates a version, waits until it is active and registers the agent environment variables. Running it again creates version 2 of the same agent.

Operate: `invoke` calls the agent endpoint with your Entra token, `monitor` streams the container's console logs, and `doctor` runs local, authentication and remote checks. The demo is verified when `doctor` ends with `0 failed`.
