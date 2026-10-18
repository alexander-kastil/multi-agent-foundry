---
slide: "03-03"
topic: 03-browser-automation
title: "Playwright workspace and connections"
subtitle: "What provision.azcli creates before an agent can browse"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/03-03-workspace-connection-setup.svg
order: 10
source: demos/03-agent-service/03-browser-automation/demo-browser-automation.md
gamma_prompt: >
  Slide titled "Playwright workspace and connections", subtitle "What provision.azcli creates
  before an agent can browse". Left half: four short bullets on the Playwright workspace
  m3-playwright, the Playwright Workspace Contributor role for the project managed identity,
  the m3-browser-mcp connection used by the Python tool and the m3-playwright-mcp connection
  used by the C# MCP tool. Right half: the supplied diagram of the Foundry project, its two
  connections and the workspace they reach, shown whole without cropping. Clean minimal
  style, white background.
---

# Playwright workspace and connections

## Content

- A **Playwright workspace** hosts the managed browsers, here `m3-playwright` in West Europe
- The project **managed identity** holds the Playwright Workspace Contributor role on it
- `m3-browser-mcp` (kind `PlaywrightWorkspace`) serves the Python **`BrowserAutomationTool`**
- `m3-playwright-mcp` (kind `remote-tool`) serves the C# **`McpTool`**

## Speaker Notes

Three things must exist before an agent can browse: a workspace, a role for the project identity and a project connection. provision.azcli creates them and prints the values that go into .env.

Workspace: Playwright Workspaces is a managed service that hosts headless browsers behind a remote MCP server. The workspace sits in West Europe while the Foundry project is in Sweden Central, which works because West Europe is on the supported list.

Managed identity: the Foundry project identity calls the workspace, so it gets the Playwright Workspace Contributor role at the workspace scope. Project managed identity is the enterprise default and avoids storing an access token.

Python connection: BrowserAutomationTool takes the full resource ID of m3-browser-mcp from BROWSER_CONNECTION_ID. The kind PlaywrightWorkspace must be written in exact PascalCase.

C# connection: the Azure.AI.Projects 2.0.x packages expose only the preview browser tool type, so the C# project attaches the same endpoint as an MCP tool. It reads the connection name m3-playwright-mcp from PLAYWRIGHT_MCP_CONNECTION.
