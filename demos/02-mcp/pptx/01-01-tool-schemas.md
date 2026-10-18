---
slide: "01-01"
topic: 01-mcp-servers
title: "Tool schemas from C# and Python"
subtitle: "What tools/list returns for search_employees in each SDK"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/01-01-tool-schemas.svg
order: 3
source: demos/02-mcp/01-mcp-servers/readme.md
gamma_prompt: >
  Slide titled "Tool schemas from C# and Python", subtitle "What tools/list returns for
  search_employees in each SDK". Left half holds four short bullets on how C# attributes and
  Python decorators turn into the tool name, annotations, description and input schema. Right
  half shows a mapping diagram: the C# attribute code and the Python decorator code on the left,
  each line linked by an arrow to the matching field of its tools/list entry on the right, with
  the two argument names searchTerm and search_term highlighted in amber. White background,
  clean sans-serif font, code in a monospace font.
---

# Tool schemas from C# and Python

## Content

- **Attributes in C#**: `[McpServerTool]` and `[Description]` set the name and the description
- **Decorators in Python**: the docstring and the type hints become description and JSON Schema
- **Annotations**: `ReadOnly = true` and `read_only_hint=True` both become `readOnlyHint`
- **Argument names**: `searchTerm` in C#, `search_term` in Python, so clients read `tools/list`

## Speaker Notes

This slide shows what a client receives when it asks the HR server for its tools. The same search_employees tool is declared once in C# and once in Python, and each SDK builds the tools/list entry from the code.

Attributes in C#: a class marked McpServerToolType is scanned, and every method marked McpServerTool becomes a tool. The description you write is the only thing the model sees when it decides which tool to call, so it is part of your API surface.

Decorators in Python: the mcp.tool decorator registers a plain function. The docstring becomes the description and the type hints become the JSON Schema.

Annotations: ReadOnly and Destructive in C# become the readOnlyHint and destructiveHint annotations. Hosts use them to decide which calls need a confirmation.

Argument names: the C# parameter is searchTerm and the Python parameter is search_term, because each SDK follows its own naming convention. A client must read the schema from tools/list instead of guessing the argument names.
