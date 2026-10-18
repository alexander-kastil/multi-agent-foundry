---
slide: "04-01"
topic: 04-middleware
title: "Three interception points"
subtitle: "What one run with one tool call passes through"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/04-01-interception-points.svg
order: 9
source: demos/04-agent-framework/04-middleware/readme.md
gamma_prompt: >
  Slide titled "Three interception points", subtitle "What one run with one tool call
  passes through". Left half holds four short bullets about agent, function and chat
  middleware in Microsoft Agent Framework. Right half shows a three-lane diagram: an agent
  lane whose bar covers the whole agent.run call, a chat lane with two model calls, and a
  function lane with one get_trail_status tool call between them, each lane labelled on the
  left with its middleware type, context object and demo middleware names. Clean minimal
  style, white background, amber for middleware, blue for the agent run and the tool, purple
  for the model calls.
---

# Three interception points

## Content

- **Agent middleware** wraps one `agent.run` call and runs once per run
- **Function middleware** wraps one tool invocation and runs once per tool call
- **Chat middleware** wraps each request to the model, including the one that sends the tool result back
- One run with one tool passes agent middleware once, function middleware once and chat middleware twice

## Speaker Notes

This slide maps the three places where Agent Framework lets you intercept an agent, using the trail-guide run from the demo that calls one tool.

Agent middleware: it receives an `AgentContext` and wraps the whole `agent.run` call, so the demo's timer and credential guardrail live here. The two `[agent]` lines in the demo surround the whole run, tool call and model calls included.

Function middleware: it receives a `FunctionInvocationContext` and runs once around each tool call. The demo's tool auditor logs the arguments of `get_trail_status` and can rewrite its result before the model reads it.

Chat middleware: it receives a `ChatContext` and runs once per request to the model, including the request that sends the tool result back. That is why the demo's model meter counts two calls for one question.

The count to remember: one run with one tool passes agent middleware once, function middleware once and chat middleware twice. The first model call asks the model, the second returns the tool result.
