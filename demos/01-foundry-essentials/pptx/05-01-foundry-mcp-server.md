---
slide: "05-01"
topic: 05-foundry-toolkit
title: "Foundry MCP server"
subtitle: "Management tools that run with your own Entra permissions"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/05-01-foundry-mcp-server.svg
order: 14
source: demos/01-foundry-essentials/05-foundry-toolkit/demo-foundry-mcp.md
gamma_prompt: >
  Slide titled "Foundry MCP server", subtitle "Management tools that run with your own
  Entra permissions". Left half holds five short bullets: a Microsoft-hosted MCP server in
  preview at mcp.ai.azure.com with 80 tools in the live list; every client sends a bearer
  token for that audience; calls run with your permissions through the Entra
  on-behalf-of flow; Reader is enough for read tools such as model_deployment_get;
  Contributor is needed to create, invoke and delete a prompt agent. Right half shows the
  supplied diagram with the Python, C# and Copilot clients on the left, the MCP server in
  the middle and the Reader and Contributor tools of the Foundry project on the right.
  Clean and light, white background.
---

# Foundry MCP server

## Content

- A Microsoft-hosted **MCP server (preview)** at `https://mcp.ai.azure.com`, 80 tools in the live list
- Every client sends a **bearer token** for the audience `https://mcp.ai.azure.com`
- Calls run with **your permissions** through the Entra on-behalf-of flow
- **Reader** is enough for read tools such as `model_deployment_get`
- **Contributor** is needed to create, invoke and delete a prompt agent

## Speaker Notes

This slide follows a tool call from steps 2 to 4 of the demo, from your code to the Foundry project. Python, C# and GitHub Copilot Agent Mode are three clients of the same server.

Hosted by Microsoft: the Foundry MCP server is a preview service at `https://mcp.ai.azure.com` that manages agents, deployments, evaluations and more. The documentation lists 79 tools and the live server returned 80, because the tool set changes during the preview.

A token for the right audience: `foundry_mcp.py` and `FoundryMcp.cs` ask `DefaultAzureCredential` for a token with the `https://mcp.ai.azure.com/.default` scope and send it as a bearer header. Copilot Agent Mode reaches the same server through `.vscode/mcp.json`.

Your identity, your roles: every call runs with your own permissions through the Microsoft Entra on-behalf-of flow. The roles you hold on the project decide which tools succeed.

Read tools: `model_deployment_get` returned the `m1-router-subset` deployment and its two subset models without a single REST call from your code. Reader is enough for this tool.

Write tools: `agent_update` creates `m1-mcp-agent`, `agent_invoke` sends it a message and `agent_delete` removes it, and that lifecycle needs Contributor. Write tools change live resources and billing at once, so review the parameters before you approve a call.
