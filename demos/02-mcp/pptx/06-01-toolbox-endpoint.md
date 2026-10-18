---
slide: "06-01"
topic: 06-foundry-toolbox
title: "The toolbox endpoint"
subtitle: "Two MCP servers behind one managed URL"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/06-01-toolbox-endpoint.svg
order: 13
source: demos/02-mcp/06-foundry-toolbox/demo-foundry-toolbox.md
gamma_prompt: >
  Slide titled "The toolbox endpoint" with subtitle "Two MCP servers behind one managed URL". Left
  half: four short bullets about the toolbox bundling two servers, the consumer endpoint with an Entra
  token, prefixed tool names and the project connection that keeps the key. Right half: the provided
  diagram of three clients calling one toolbox that routes to the Microsoft Learn server and, through a
  project connection, to a function app, at full legibility on white. Use exactly the one image given
  for this card; do not generate or add any other images, icons or logos.
---

# The toolbox endpoint

## Content

- The toolbox `m2-docs-and-snippets` bundles the **Microsoft Learn** server and the **snippets** function app
- Every client calls the **consumer endpoint** with an Entra token for `https://ai.azure.com/.default`
- Tool names start with the **server label** and three underscores, as in `snippets___get_snippet`
- The snippets tools go through the **project connection**, so no client sees the function key

## Speaker Notes

This slide shows what `create_toolbox.py` builds and how every client reaches it. Instead of each agent carrying tool definitions, credentials and approval rules, the toolbox holds them in one place.

Two servers in one toolbox: the Learn server needs no credentials, and the snippets server is the function app from the previous topic. Give every tool a `description`, because the model uses it to choose.

One endpoint for every client: the Python agent with `FoundryToolbox`, the plain MCP client in `list_tools.py` and the .NET host with `AddFoundryToolboxes` all call the same URL. The identity needs the Foundry User role on the project.

Prefixed names: the endpoint lists `snippets___get_snippet` with three underscores between label and tool, because several servers can expose the same tool name. Read the names from `tools/list` instead of building them.

The key stays in the connection: the snippets tool references `m2-func-mcp-key`, which adds the `x-functions-key` header. The agent code contains no server URL and no key.
