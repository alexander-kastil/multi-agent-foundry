---
slide: "01-02"
topic: 01-workflows
title: "Executors and agent nodes"
subtitle: "An agent takes the same place in the graph as a function"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/01-02-executors-agent-nodes.svg
order: 4
source: demos/05-orchestration/01-workflows/demo-workflows.md
gamma_prompt: >
  Slide titled "Executors and agent nodes", subtitle "An agent takes the same place in the graph as a
  function". Left half holds four short bullets about typed executors and agent nodes. Right half
  shows the supplied diagram uncropped: two vertical workflows side by side, a code pipeline
  parse_invoice, add_vat, format_receipt and a second graph parse_invoice, the invoice_reviewer agent
  in purple, publish_decision, each edge labelled with its message type and both ending in a green
  result.get_outputs() box. White background, clean minimal style.
---

# Executors and agent nodes

## Content

- The **type contract**: `WorkflowContext[Invoice]` sends invoices, `WorkflowContext[Never, str]` yields text
- `send_message` feeds the next node, `yield_output` reaches the caller of `workflow.run`
- An **agent** passed to `add_edge` becomes an agent executor, fed by an `AgentExecutorRequest`
- `build()` checks that the message types match across every edge

## Speaker Notes

This slide puts Step 1 and Step 4 of the demo side by side. Both graphs have the same shape; only the middle node changes from a function to an agent.

The type contract: the type argument of `WorkflowContext` says what an executor may send or yield. `WorkflowContext[Invoice]` sends `Invoice` messages, and `WorkflowContext[Never, str]` sends nothing downstream and yields `str` outputs.

Sending and yielding: `ctx.send_message` feeds the next executor, and `ctx.yield_output` surfaces a result to whoever called `workflow.run`. `output_from` on the builder decides which executors' outputs show up in `result.get_outputs()`.

Agents as nodes: pass an agent to `add_edge` and the framework wraps it in an agent executor. The upstream executor sends an `AgentExecutorRequest` so the agent sees a real user message, and the downstream executor reads the reply from `AgentExecutorResponse`.

Validation: `build()` checks that message types match across each edge and that every executor is reachable from the start. A mismatched handler fails at build time, not in the middle of a run.
