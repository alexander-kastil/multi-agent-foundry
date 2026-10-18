---
slide: "04-01"
topic: 04-azure-functions
title: "MCP tool triggers"
subtitle: "How a decorated function becomes a tool and reads its blob"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/04-01-function-to-tool.svg
order: 9
source: demos/02-mcp/04-azure-functions/readme.md
gamma_prompt: >
  Slide titled "MCP tool triggers" with subtitle "How a decorated function becomes a tool and reads its blob".
  Left half: four short bullets about the mcp_tool decorator, the host answering tools/list, the
  mcptoolargs blob binding and a function without storage code. Right half: the provided diagram of a
  Python function mapped to its tool definition, above a tools/call that resolves a blob path, at full
  legibility on white. Use exactly the one image given for this card; do not generate or add any other
  images, icons or logos.
---

# MCP tool triggers

## Content

- **`mcp_tool`** turns a function into a tool: its name, docstring and parameters become the definition
- The host answers **`tools/list`** and runs the matching function for each `tools/call`
- **`{mcptoolargs.snippetname}`** puts a tool argument into a normal blob binding
- No storage code: the function receives the blob content and returns it

## Speaker Notes

This slide opens the Functions topic with the extension model. The function app speaks MCP for you, so the code holds nothing but functions.

The decorator: in Python, `mcp_tool` makes a function a tool, and the function name, docstring and parameters become the tool name, description and input schema. In C#, the isolated worker uses `McpToolTrigger` and `McpToolProperty` for the same job.

The host: it answers `tools/list` from the function signatures and runs the matching function for each `tools/call`. There is no server framework in your code.

The binding: `{mcptoolargs.snippetname}` puts the tool argument into the blob path, so `greeting` resolves to `snippets/greeting.json`. Without the `mcptoolargs.` prefix the binding fails with `No value for named parameter`.

No storage code: the function receives the blob content and returns it, which is why the Inspector call returns `print(1)`. In the local run the blob lives in Azurite.
