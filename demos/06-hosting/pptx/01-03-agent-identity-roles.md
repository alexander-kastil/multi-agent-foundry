---
slide: "01-03"
topic: 01-hosted-agents
title: "Agent identity and roles"
subtitle: "Who needs which role, and what the agent's identity can reach"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/01-03-agent-identity-roles.svg
order: 5
source: demos/06-hosting/01-hosted-agents/readme.md
gamma_prompt: >
  Slide titled "Agent identity and roles", subtitle "Who needs which role, and what the agent's identity can
  reach". The left half holds four short bullets: people need Foundry Project Manager to deploy and Foundry Agent
  Consumer to call; the agent identity is a service principal per agent that the container runs as; the project
  managed identity does platform work such as pulling the image; models work through the project endpoint while
  your own resources need a role you assign. The right half shows the supplied diagram unchanged: a deploy-time
  row (you, the Foundry project with its managed identity, Azure Container Registry) above a run-time row (caller,
  session sandbox with the amber agent identity, Foundry model, your Storage account). White background, clean
  sans-serif type, no extra imagery.
---

# Agent identity and roles

## Content

- People need two roles: **Foundry Project Manager** to deploy, Foundry Agent Consumer to call
- The **agent identity** is a service principal per agent that your container runs as
- The **project managed identity** does platform work such as pulling the image
- Models work through the project endpoint; your own resources need a **role you assign**

## Speaker Notes

Two identities and two roles for people meet around every hosted agent. The diagram splits them into deploy time and run time.

Foundry Project Manager: you need it at the project scope to deploy, and callers of the endpoint need Foundry Agent Consumer or a role that includes endpoint access. These roles were renamed from the Azure AI roles with the same role ids, so older portals may still show the old names.

Agent identity: every hosted agent gets its own Microsoft Entra identity at deploy time, a service principal you can see with `az ad sp show`. It is what your container authenticates as at run time.

Project managed identity: one identity covers the whole project and does platform work such as pulling the image from the registry. On the container path it needs Container Registry Repository Reader.

Role you assign: by default the agent identity can call models through the project endpoint and use session storage. In step 6 of the demo it holds no role assignments, so reading your own Storage account needs a role such as Storage Blob Data Reader.
