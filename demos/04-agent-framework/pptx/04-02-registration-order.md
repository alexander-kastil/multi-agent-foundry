---
slide: "04-02"
topic: 04-middleware
title: "Registration scope and order"
subtitle: "Agent-level middleware wraps run-level middleware"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/04-02-registration-order.svg
order: 10
source: demos/04-agent-framework/04-middleware/demo-middleware.md
gamma_prompt: >
  Slide titled "Registration scope and order", subtitle "Agent-level middleware wraps
  run-level middleware". Left half holds four short bullets about where middleware is
  registered and the order it runs in. Right half shows four amber middleware columns A1,
  A2, R1 and R2 followed by a blue column for the agent, with arrows running right through
  the top halves (code before call_next) and back left through the bottom halves (code after
  call_next), and brackets above marking A1 and A2 as agent level and R1 and R2 as run level.
  Clean minimal style, white background.
---

# Registration scope and order

## Content

- Middleware registers on the **Agent** for every run, or on a single `agent.run` call
- Code before `await call_next()` runs on the way in, code after it on the way out
- The order is A1, A2, R1, R2, the agent, then back out through R2, R1, A2, A1
- Policies that must always apply go on the agent, per-request behavior on the run

## Speaker Notes

After the three layers, this slide shows what happens when several middleware functions share a layer and come from two places.

Two scopes: middleware registers on the `Agent` or on a single run. Agent-level middleware wraps run-level middleware.

Way in and way out: everything before `await call_next()` runs on the way in, everything after it runs on the way out. `call_next()` takes no argument, because middleware changes the shared context object in place.

The order: the four tracers in the demo print A1, A2, R1, R2 going in and R2, R1, A2, A1 coming out. Function and chat middleware follow the same wrapping when the tool or model call happens.

Where to put what: policies that must always apply belong at the agent level. Per-request behavior belongs at the run level.
