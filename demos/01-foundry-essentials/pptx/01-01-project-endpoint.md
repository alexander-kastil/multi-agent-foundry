---
slide: "01-01"
topic: 01-foundry-projects
title: "The project endpoint"
subtitle: "One URL names the resource and the project"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/01-01-project-endpoint.svg
order: 3
source: demos/01-foundry-essentials/01-foundry-projects/readme.md
gamma_prompt: >
  Slide titled "The project endpoint", subtitle "One URL names the resource and the
  project". Left half holds four short bullets: the endpoint lives in PROJECT_ENDPOINT,
  its host starts with the Foundry resource name and its last path segment is the
  project, the OpenAI-compatible base URL and the Responses API URL are derived from it,
  and the Foundry SDK, agents, evaluations and the Foundry MCP server all start from it.
  Right half shows the supplied diagram that splits the endpoint URL into colour-coded
  segments with the derived URLs and the consumers below. Clean and light, white
  background.
---

# The project endpoint

## Content

- **PROJECT_ENDPOINT** in `.env` is the only value that identifies your project in code
- The host starts with the **Foundry resource** name; the last path segment is the **project**
- `/openai/v1/` and `/openai/v1/responses` are **derived** from the same value
- The Foundry SDK, the Responses API, agents, evaluations and the Foundry MCP server all start from it

## Speaker Notes

Every agent in this class runs inside one Foundry project, and code reaches it through one URL. This slide takes that URL apart so it stops being an opaque string.

One value in .env: the class projects keep the endpoint in PROJECT_ENDPOINT next to the script. You read it back from the project with the Azure CLI, so you never assemble it by hand.

Resource and project: the first label of the host is the Foundry resource, and the last path segment is the project. endpoint.py in the demo splits the URL exactly this way.

Derived URLs: the OpenAI-compatible client uses the endpoint plus /openai/v1/ as its base_url, and the Responses API lives at /openai/v1/responses. That is why one value is enough.

Everything starts here: on the new platform the Foundry SDK, the Responses API, agents, evaluations and the Foundry MCP server all start from the project endpoint.
