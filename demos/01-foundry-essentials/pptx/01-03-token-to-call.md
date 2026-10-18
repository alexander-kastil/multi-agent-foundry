---
slide: "01-03"
topic: 01-foundry-projects
title: "From sign-in to the first call"
subtitle: "Credential, token, role assignment and project"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/01-03-token-to-call.svg
order: 5
source: demos/01-foundry-essentials/01-foundry-projects/demo-project-inspector.md
gamma_prompt: >
  Slide titled "From sign-in to the first call", subtitle "Credential, token, role
  assignment and project". Left half holds four short bullets: DefaultAzureCredential
  uses the az login session locally and a managed identity in Azure; the access token
  has the audience https://ai.azure.com; a Foundry User assignment at the resource,
  resource group or subscription flows down to the project; the first data plane call
  prints output ready without any key. Right half shows the supplied diagram of the
  credential and token on the left and the nested subscription, resource group, Foundry
  resource and project on the right. Clean and light, white background.
---

# From sign-in to the first call

## Content

- **DefaultAzureCredential** uses your `az login` session locally and a managed identity in Azure
- The access token's **audience** is `https://ai.azure.com`
- A **role assignment** at the resource, resource group or subscription flows down to the project
- The **first data plane call** prints `output   : ready` without any key

## Speaker Notes

This slide follows one call from your sign-in to the project. It is the chain the demo checks in steps 4 and 5.

DefaultAzureCredential: it picks up your az login session locally and a managed identity in Azure. All samples in this class use it, so the same code runs in both places.

Audience: the token it requests has the audience https://ai.azure.com. identity.py decodes the token locally to show that audience and who the token identifies.

Role assignment: assignments at the resource, resource group or subscription scope flow down to the project. Any row for Foundry User, Foundry Project Manager or Foundry Owner lets your code call the project.

First call: smoke.py sends one Responses request through the project and prints output ready. Keys also work on some endpoints, but they bypass RBAC, so the class does not use them.
