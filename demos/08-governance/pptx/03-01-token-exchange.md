---
slide: "03-01"
topic: 03-agent-identity
title: "Token exchange for a tool call"
subtitle: "Four stages from the blueprint to the MCP server"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/03-01-token-exchange.svg
order: 7
source: demos/08-governance/03-agent-identity/readme.md
gamma_prompt: >
  Slide titled "Token exchange for a tool call", subtitle "Four stages from the
  blueprint to the MCP server". Left half: five short bullets, one per stage of the
  token exchange plus where platform and tool roles belong. Right half: a diagram with
  four numbered amber stage circles on a timeline above four cards (project managed
  identity, agent identity, Microsoft Entra ID, MCP server or A2A endpoint) and two
  wide cards below for platform roles and tool roles. Clean minimal style, white
  background, show the whole diagram without cropping.
---

# Token exchange for a tool call

## Content

- The **project managed identity** authenticates the blueprint with a federated credential
- Agent Service receives a token for the **agent identity**
- Entra issues an access token for the **audience** of the downstream service
- The **MCP server or A2A endpoint** receives that token
- Platform roles go to the project identity, **tool roles** to the agent identity

## Speaker Notes

Agent Service runs this token exchange for every tool call, and the two identities in it do different jobs. Mixing them up is the most common cause of tool authorization failures.

Project managed identity: there is one per project, of type `ManagedIdentity`. It authenticates the blueprint to Entra through a federated credential, so no secret is stored.

Agent identity: a service principal of type `ServiceIdentity`, one per agent, created from the blueprint. It stays the same when you add versions.

Audience: the resource identifier of the target service, for example `https://storage.azure.com`, not the URL of the MCP server. A wrong audience fails even when the roles are correct.

MCP server or A2A endpoint: the token arrives there, and the agent identity is the principal that needs the role on the target resource.

Tool roles: the managed identity covers model inference, container images and telemetry, and it never reaches the downstream resource. In the demo you look up both identities in Entra and see the two types side by side.
