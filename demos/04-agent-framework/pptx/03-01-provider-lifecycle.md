---
slide: "03-01"
topic: 03-context-providers
title: "The provider lifecycle"
subtitle: "before_run builds the request, after_run stores what happened"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/03-01-provider-lifecycle.svg
order: 7
source: demos/04-agent-framework/03-context-providers/readme.md
gamma_prompt: >
  Slide titled "The provider lifecycle", subtitle "before_run builds the request,
  after_run stores what happened". Left half holds four short bullets on Microsoft
  Agent Framework context providers: they are attached through context_providers and
  run in list order, before_run adds instructions, messages, tools or middleware to
  the SessionContext, after_run stores the result, and provider state lives in
  session.state per source_id. Right half shows the supplied pipeline diagram from
  agent.run through before_run, SessionContext, the model call and after_run into
  session.state and back to the next run. Clean minimal style, white background, no
  extra imagery.
---

# The provider lifecycle

## Content

- Providers attach through **context_providers** and run in list order
- `before_run` adds instructions, messages, tools or middleware to the `SessionContext`
- `after_run` stores what happened once the model call returns
- Provider state lives in **session.state**, one slice per `source_id`

## Speaker Notes

A model only knows what is in the request, and context providers build that request. History, user preferences and long-term memory are all providers, so one list on the agent controls all three.

context_providers: you pass the list when you create the agent, and each provider has a unique `source_id`. The agent calls the providers in list order.

before_run: inside the hook the `SessionContext` takes contributions through `extend_instructions`, `extend_messages`, `extend_tools` and `extend_middleware`. In the demo the `primary` history provider replays the transcript, while `audit` loads nothing.

after_run: it runs after the model call and stores what happened. The `audit` provider also records the context other providers injected, which makes it a useful audit trail.

session.state: each provider keeps its own slice, so the demo prints `memory: 4 messages` and `audit: 4 messages`. Because the state sits inside `AgentSession.state`, it survives `to_dict()` and `from_dict()` without extra work.
