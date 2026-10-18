---
slide: "02-02"
topic: 02-builtin-tools
title: "Where the built-in tools run"
subtitle: "Sandbox, vector store and public web"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/02-02-tool-reach.svg
order: 7
source: demos/03-agent-service/02-builtin-tools/readme.md
gamma_prompt: >
  Slide titled "Where the built-in tools run", subtitle "Sandbox, vector store and public
  web". Left half: four short bullets on tools being part of the agent definition, Code
  Interpreter running Python in a sandbox without outbound network, File Search over one
  vector store, and Web Search reaching the public web outside the Azure compliance
  boundary. Right half: the supplied diagram of an agent version, its three tools and what
  each tool reaches, with a dashed compliance boundary, shown whole without cropping. Clean
  minimal style, white background.
---

# Where the built-in tools run

## Content

- A tool is part of the **agent definition** and is versioned with the instructions
- **Code Interpreter** runs Python in a sandbox in your project's region, with no outbound network
- **File Search** chunks, embeds and searches the files in one vector store
- **Web Search** needs no Bing resource, and the query leaves the Azure compliance boundary

## Speaker Notes

Built-in tools need no tool server and no function loop: you attach them to an agent version and Foundry Agent Service runs them. This slide shows what each tool reaches.

Agent definition: the tools sit next to the model and the instructions, so changing a tool means a new version. The model then picks the tools per question.

Code Interpreter: the sandbox runs in the same region as your project and cannot make outbound network requests. The demo uploads quarterly_results.csv with the files API and passes its id in an automatic container.

File Search: you create a vector store, upload files and attach the store, and the service chunks, embeds and searches them. An agent can use at most one vector store, which holds up to 10,000 files.

Web Search: it needs no Bing resource and no project connection, and it sends data outside the Azure compliance boundary. Review that before you use it with regulated data.
