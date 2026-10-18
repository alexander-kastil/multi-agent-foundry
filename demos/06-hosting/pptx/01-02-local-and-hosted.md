---
slide: "01-02"
topic: 01-hosted-agents
title: "Running the agent locally and hosted"
subtitle: "The same main.py, with settings the platform injects"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/01-02-local-and-hosted.svg
order: 4
source: demos/06-hosting/01-hosted-agents/demo-hosted-agents.md
gamma_prompt: >
  Slide titled "Running the agent locally and hosted", subtitle "The same main.py, with settings the platform
  injects". The left half holds four short bullets that follow one support-desk agent from a laptop to Foundry:
  ResponsesHostServer serves main.py on port 8088 with settings from .env; the same code builds into a
  linux/amd64 container image; the demo deploys a ZIP with create_version_from_code and remote_build; once hosted,
  the platform injects the FOUNDRY_* settings and you declare only the model deployment name. The right half shows
  the supplied diagram unchanged: a blue column for your machine, two route boxes in the middle (grey container
  image, amber ZIP) and a purple session sandbox column listing the injected variables. White background, clean
  sans-serif type, no extra imagery.
---

# Running the agent locally and hosted

## Content

- `ResponsesHostServer` serves `main.py` on **port 8088** with settings from `.env`
- The same code builds into a **container image** for linux/amd64
- The demo deploys a **ZIP** with `create_version_from_code`, and `remote_build` installs the packages
- Once hosted, the platform **injects** the `FOUNDRY_*` settings; you declare only the model deployment name

## Speaker Notes

This slide follows the demo from the laptop to the hosted agent. The code stays the same at every step; only its surroundings change.

Port 8088: `main.py` is a normal Agent Framework agent, and `ResponsesHostServer` serves it on port 8088 with the project endpoint and model from `.env`. Locally, the `runtime_info` tool reports no injected platform variables.

Container image: step 3 builds the same code as a linux/amd64 image and checks `/readiness`. Pushing it to Azure Container Registry and setting `container_configuration.image` is the other deployment route.

ZIP: `deploy.py` zips `main.py` and `requirements.txt`, uploads them with `create_version_from_code` and polls until the version is active. With `remote_build` the platform installs `requirements.txt`, so this route needs neither Docker nor a registry.

Injects: in the sandbox the same code sees the project endpoint, the telemetry connection string and its own name and version, none of which you set. The model deployment name is the one setting you declare yourself, in `environment_variables`.
