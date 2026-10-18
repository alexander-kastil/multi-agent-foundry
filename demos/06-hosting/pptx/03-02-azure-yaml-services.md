---
slide: "03-02"
topic: 03-azd-ai-agent
title: "Services in azure.yaml"
subtitle: "An agent service that uses a project service"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/03-02-azure-yaml-services.svg
order: 9
source: demos/06-hosting/03-azd-ai-agent/demo-azd-ai-agent.md
gamma_prompt: >
  Slide titled "Services in azure.yaml", subtitle "An agent service that uses a project service". The left half
  holds four short bullets on the file that azd ai agent init writes: the agent service has host azure.ai.agent,
  kind hosted and a codeConfiguration block; it uses a project service with host azure.ai.project that holds the
  project endpoint; env maps the model deployment name and the endpoint reaches the agent as
  FOUNDRY_PROJECT_ENDPOINT; deploying under an existing agent name creates a new version. The right half shows the
  supplied diagram unchanged: a blue azure.yaml panel with both services in monospace type, an azd deploy arrow to
  an amber hosted agent box, and an arrow to a purple Foundry project box. White background, clean sans-serif
  type, no extra imagery.
---

# Services in azure.yaml

## Content

- The agent service has `host: azure.ai.agent`, `kind: hosted` and a **codeConfiguration** block
- It uses a **project service** with `host: azure.ai.project` that holds the project endpoint
- `env` maps the **model deployment name**; the endpoint reaches the agent as `FOUNDRY_PROJECT_ENDPOINT`
- Deploying under an existing **agent name** creates a new version

## Speaker Notes

`init` writes a single `azure.yaml`, and this slide maps its two services to what `azd deploy` creates in Foundry.

codeConfiguration: the agent is a service with host `azure.ai.agent` and kind `hosted`, and its `codeConfiguration` names the runtime, the entry point and `remote_build`. `azd deploy` turns it into the hosted agent `m6-azd-agent`.

Project service: the second service, host `azure.ai.project`, holds the endpoint of the project chosen with `--project-id`. Passing `--model-deployment` reuses the `gpt-5.4` deployment, so nothing new is provisioned for the model.

Model deployment name: `env` maps it into the agent as `AZURE_AI_MODEL_DEPLOYMENT_NAME`, and the project endpoint reaches the agent as `FOUNDRY_PROJECT_ENDPOINT`.

Agent name: the agent name is the Foundry agent identity, and Foundry agents are unique by name within a project. Deploying under an existing name creates a new version instead of a second agent.
