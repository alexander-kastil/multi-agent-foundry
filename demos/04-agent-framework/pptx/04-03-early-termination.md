---
slide: "04-03"
topic: 04-middleware
title: "Stopping a run early"
subtitle: "A guardrail answers before the model is called"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/04-03-early-termination.svg
order: 11
source: demos/04-agent-framework/04-middleware/demo-middleware.md
gamma_prompt: >
  Slide titled "Stopping a run early", subtitle "A guardrail answers before the model is
  called". Left half holds four short bullets about ending an agent run from middleware.
  Right half shows two rows: in the top row a trail question passes an amber guardrail box,
  reaches a purple model call and ends in a green answer; in the bottom row a question about
  an admin password stops at the guardrail, which sets context.result and raises
  MiddlewareTermination, and a long arrow skips the model straight to a green refusal.
  Clean minimal style, white background.
---

# Stopping a run early

## Content

- The guardrail reads the last user message and looks for a blocked word
- To refuse, it sets **context.result** and raises `MiddlewareTermination`
- The rest of the chain and the model call are skipped, and the caller receives that result
- Prefer the agent layer: stopping inside a tool loop can leave a tool call without a result

## Speaker Notes

This slide shows how a middleware ends a run before the model sees the request.

The check: `GuardrailMiddleware` is a class-based agent middleware that reads the last user message and looks for a blocked word. The trail question passes, so it prints `[guardrail] passed` and awaits `call_next()`.

The refusal: for the question about the admin password it sets `context.result` to an `AgentResponse` and raises `MiddlewareTermination`. The caller receives the answer that the agent can't help with credentials or secrets.

What is skipped: the rest of the chain and the model call never run. In the C# run the blocked prompt prints no `[chat]` lines, because the guardrail ended the run before any model call.

Where to stop: the same pattern works for function and chat middleware, but terminating a tool loop can leave history with a tool call that has no result. Prefer the agent layer; in C# the guardrail returns a new `AgentResponse` instead of raising an exception.
