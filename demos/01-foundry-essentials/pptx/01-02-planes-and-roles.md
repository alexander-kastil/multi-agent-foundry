---
slide: "01-02"
topic: 01-foundry-projects
title: "Control plane and data plane roles"
subtitle: "Which Foundry role covers which actions"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/01-02-planes-and-roles.svg
order: 4
source: demos/01-foundry-essentials/01-foundry-projects/readme.md
gamma_prompt: >
  Slide titled "Control plane and data plane roles", subtitle "Which Foundry role covers
  which actions". Left half holds five short bullets: the control plane creates
  deployments and projects while the data plane builds agents, runs evaluations and
  uploads files; Foundry User at the project scope is enough for day-to-day developers;
  Foundry Account Owner works on the control plane and Foundry Owner covers both;
  Foundry Agent Consumer only calls agents; scripts assign roles by their unchanged
  definition IDs. Right half shows the supplied diagram with one row per role and a bar
  under the plane it covers. Clean and light, white background.
---

# Control plane and data plane roles

## Content

- The **control plane** creates deployments and projects; the **data plane** builds agents, runs evaluations and uploads files
- **Foundry User** at the project scope is all a day-to-day developer needs
- **Foundry Account Owner** manages accounts, projects and models; **Foundry Owner** covers both planes
- **Foundry Agent Consumer** is the narrower role for principals that only call agents
- Scripts assign roles by **role definition ID**, which stayed the same through the rename

## Speaker Notes

After the endpoint, the next question is what your identity may do with it. Azure RBAC splits Foundry actions into two planes, and each Foundry role covers one or both.

Two planes: control plane actions create deployments and projects, and data plane actions build agents, run evaluations and upload files. Every role on the slide maps onto this split.

Foundry User: it builds and tests agents in a project and calls models. Day-to-day developers need it at the project scope and nothing more.

Account Owner and Owner: Foundry Account Owner creates accounts and projects and manages models, which is control plane work. Foundry Owner does everything above for a self-serve owner.

Agent Consumer: a principal that only calls agents can use the narrower Foundry Agent Consumer role. Foundry Project Manager sits between, developing and publishing agents and assigning Foundry User.

IDs in scripts: the roles were renamed from the Azure AI names, and the portal still shows the former names in places. The definition IDs did not change, so scripts that use the GUID keep working through the rollout.
