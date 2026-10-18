---
slide: "02-01"
topic: 02-builtin-tools
title: "Tool calls in the response output"
subtitle: "Each demo step prints its tool call items before the message"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/02-01-tool-output-items.svg
order: 6
source: demos/03-agent-service/02-builtin-tools/demo-builtin-tools.md
gamma_prompt: >
  Slide titled "Tool calls in the response output", subtitle "Each demo step prints its tool
  call items before the message". Left half: four short bullets on item types in
  response.output, three Code Interpreter calls with a container file citation, file and
  URL citations, and a combined agent that used two tools in sequence. Right half: the
  supplied diagram of four rows of output items with the citation each final message
  carries, shown whole without cropping. Clean minimal style, white background.
---

# Tool calls in the response output

## Content

- Every item in `response.output` has a **type**, and a tool run comes before the `message`
- `m3-analyst` ran Code Interpreter **three times** and returned a `container_file_citation`
- File Search answers carry a `file_citation`, Web Search answers a **`url_citation`**
- The **combined agent** printed `file_search_call`, `code_interpreter_call`, `message`

## Speaker Notes

This slide is the evidence trail of the demo: what response.output held in each of the four steps. Printing the item types is the quickest way to prove which tool ran.

Item types: when the model decides to use a tool, the service runs it and adds a tool call item to the output. The final message item carries the answer and its citations.

Code Interpreter: three code_interpreter_call items mean the model iterated three times before it answered. The container_file_citation on the last message names the chart, which you download with its container id and file id.

Citations: the warranty and return terms exist only in product-info.md, so the file_citation shows the answer came from your document. The url_citation from Web Search lets you check the source yourself.

Combined agent: m3-combined-agent found the two-year warranty with File Search and computed 2 × 365 = 730 with Python. The tools used line shows that the model chose both tools in sequence.
