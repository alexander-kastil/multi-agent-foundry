---
slide: "04-02"
topic: 04-foundry-iq
title: "Connecting an agent to a knowledge base"
subtitle: "The MCP tool, the project connection and two role assignments"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/04-02-knowledge-base-connection.svg
order: 12
source: demos/03-agent-service/04-foundry-iq/readme.md
gamma_prompt: >
  Slide titled "Connecting an agent to a knowledge base", subtitle "The MCP tool, the project
  connection and two role assignments". Four short bullets on the left, a large architecture
  diagram on the right: a Foundry resource holding a project with an agent, its MCP tool and a
  project connection, plus a gpt-5.4 deployment; an Azure AI Search service holding the knowledge
  base, two knowledge sources and two indexes. One arrow carries the MCP call with the project
  identity, the other the model call with the search service identity, each labelled with its
  role. White background, blue for the agent, purple for service objects, amber for the knowledge
  base.
---

# Connecting an agent to a knowledge base

## Content

- The agent gets one **MCPTool** restricted to `knowledge_base_retrieve`
- The project connection `m3-outdoor-kb-mcp` is a **RemoteTool** connection with the project managed identity
- The project identity needs **Search Index Data Reader** on the search service
- The search service identity needs **Cognitive Services User** on the Foundry resource

## Speaker Notes

A knowledge base lives on Azure AI Search, and an agent in Foundry reaches it over MCP. The diagram shows every object the demo creates on that path and the two identities that make the calls work.

MCPTool: the agent version carries one MCP tool with the server label `knowledge-base` and `allowed_tools` set to `knowledge_base_retrieve`, the only knowledge base tool Foundry Agent Service supports. Python builds it with `MCPTool`, C# with `ResponseTool.CreateMcpTool`.

Project connection: `m3-outdoor-kb-mcp` is a `RemoteTool` connection that signs in with the project managed identity for the audience `https://search.azure.com/`. Its target is the knowledge base MCP endpoint, `/knowledgebases/m3-outdoor-kb/mcp`.

Search Index Data Reader: the project identity needs this role on the search service. The MCP call runs as that identity.

Cognitive Services User: the search service calls `gpt-5.4` for query planning and answer synthesis with its own managed identity. Without this role on the Foundry resource, creating the knowledge base fails with an authorization error from the model.
