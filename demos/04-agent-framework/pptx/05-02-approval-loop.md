---
slide: "05-02"
topic: 05-tools-approvals
title: "Approving a tool call"
subtitle: "The run stops until a human answers on the same session"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/05-02-approval-loop.svg
order: 13
source: demos/04-agent-framework/05-tools-approvals/demo-tools-approvals.md
gamma_prompt: >
  Slide titled "Approving a tool call", subtitle "The run stops until a human answers on the
  same session". Left half holds five short bullets about tool approval in Microsoft Agent
  Framework. Right half shows a loop diagram: a blue box asking to book a mountain hut leads
  to an amber box where the agent stops before the call, then to a grey box where a human
  approves or rejects, then to a blue box that answers on the same session and loops back
  to the amber box while requests remain; a green box on the right holds the final answer
  once no request is left. Clean minimal style, white background.
---

# Approving a tool call

## Content

- `approval_mode="always_require"` stops the agent before the tool executes
- The pending call comes back in **result.user_input_requests**
- A human approves or rejects, and `to_function_approval_response` sends the answer on the same session
- Loop until no request is left, because the model may ask again after a rejection
- The confirmation `HB-2935` appears only after an approve

## Speaker Notes

This slide walks through the loop that keeps a human in control of `book_hut`, a tool that charges the customer and cannot be undone.

The stop: `book_hut` uses `approval_mode="always_require"`, so the agent stops before the call. It returns the pending call in `result.user_input_requests` instead of running the function.

The decision: the script prints the approval request with hut, nights and persons, and a human approves or rejects. The answer goes back as `to_function_approval_response` in a user `Message`, sent with `agent.run` on the same session.

Same session: an approval response only authorizes a call that matches a pending request recorded in that `AgentSession`. The framework ignores unbound responses and logs a warning.

The loop: a rejected call is reported back to the model, which may ask again, so the code loops until no request is left. The confirmation `HB-2935` appears only on the approve path, which proves the function body ran only after the yes.
