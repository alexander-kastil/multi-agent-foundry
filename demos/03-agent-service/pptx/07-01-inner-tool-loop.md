---
slide: "07-01"
topic: 07-agentic-loops
title: "The inner tool loop"
subtitle: "The agent asks for the tool, your code runs it"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/07-01-inner-tool-loop.svg
order: 18
source: demos/03-agent-service/07-agentic-loops/readme.md
gamma_prompt: >
  Slide titled "The inner tool loop", subtitle "The agent asks for the tool, your code runs it". Two columns.
  Left column: the bullets. Right column, wider than the left: the provided diagram of the run_turn loop, in
  which your code sends a request, Foundry Agent Service runs the agent, your code checks for function_call
  items, runs get_market_data locally and sends back function_call_output until the loop ends, shown as an
  inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used
  as a background or side accent. Use exactly the one image given for this card; do not generate or add any
  other images, icons or logos.
---

# The inner tool loop

## Content

- The agent decides when to call `get_market_data`, and **your code** runs it
- Each `function_call` is answered with a `function_call_output` carrying the same **`call_id`**
- The loop ends when a response holds **no function call**
- Every outer loop in this topic calls this one `run_turn`

## Speaker Notes

This slide shows the loop that every later pattern in the topic builds on, before any planning or reflection is added.

Your code runs the tool: the service returns a function_call item but never executes it. Your code calls get_market_data with the arguments, and the prices come from a local dictionary, so the results are deterministic.

The call_id ties the answer to the request: each function_call_output goes back with the call_id of the call it answers. The follow-up request is chained with previous_response_id.

The stopping condition: when a response contains no function call, run_turn returns it. The highlight on the diagram walks one tool round and then the exit.

One function for every outer loop: plan then act, reflection, the fork and the resume all call run_turn. That is why the rest of the code stays short.
