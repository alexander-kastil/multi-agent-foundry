---
slide: "01-02"
topic: 01-guardrails
title: "Block signals in the response"
subtitle: "A blocked input fails with 400, a blocked tool response returns 200"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/01-02-block-signals.svg
order: 4
source: demos/08-governance/01-guardrails/demo-guardrails.md
gamma_prompt: >
  Slide titled "Block signals in the response", subtitle "A blocked input fails with
  400, a blocked tool response returns 200". Left half: five short bullets comparing
  a jailbreak stopped at user input with an indirect attack stopped at the tool
  response. Right half: a two-row diagram; the top row ends in a green HTTP 400 box
  with code content_filter, the bottom row ends in a green HTTP 200 box with status
  incomplete and post_tool blocked True. Clean minimal style, white background, show
  the whole diagram without cropping.
---

# Block signals in the response

## Content

- A jailbreak stops at **user input** before the model runs
- The call fails with **HTTP 400** and the code `content_filter`
- An injected instruction in a tool result stops at the **tool response**
- That run returns **HTTP 200** with `status=incomplete` and `post_tool blocked=True`
- Your client reads **both signals**

## Speaker Notes

In the demo one guardrail stops two attacks at two intervention points, and the client sees two different signals. This slide puts them side by side.

User input: the jailbreak in Step 2 is caught by the jailbreak control on `Prompt`, before the model runs.

HTTP 400: the error body names the intervention point and the risk, so you can show the user a specific message.

Tool response: in Step 3 the user only asks to decode a base64 string, and the `decode_text` tool returns an injected instruction. The indirect attack control on `PostToolCall` stops it.

HTTP 200: the run ends as `incomplete` with the reason `content_filter`, and the `post_tool` filter entry reports `blocked=True`.

Both signals: a client that checks only the status code misses the second block. A 200 response is not proof that the content passed.
