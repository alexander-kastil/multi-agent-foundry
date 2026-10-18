---
slide: "02-01"
topic: 02-routines
title: "How a routine runs"
subtitle: "A trigger fires, the action calls one agent, the run lands in the history"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/02-01-routine-run-flow.svg
order: 6
source: demos/06-hosting/02-routines/demo-routines.md
gamma_prompt: >
  Slide titled "How a routine runs", subtitle "A trigger fires, the action calls one agent, the run lands in the
  history". The left half holds four short bullets on Foundry routines: exactly one trigger (timer, schedule or
  github_issue); exactly one action that calls one prompt agent or hosted agent; a dispatch that starts a test run
  now, even on a disabled routine when it carries a payload; a run history that records each run's source, phase
  and response id. The right half shows the supplied diagram unchanged: grey trigger boxes and a dashed blue
  dispatch box on the left feed an amber routine box, which invokes a purple agent box, whose response id lands in
  a green run history with three rows. White background, clean sans-serif type, no extra imagery.
---

# How a routine runs

## Content

- Exactly **one trigger**: `timer` fires once, `schedule` follows a cron expression, `github_issue` reacts to an issue
- Exactly **one action**: `invoke_agent_responses_api` calls one prompt agent or hosted agent
- A **dispatch** starts a test run now, even on a disabled routine when it carries a payload
- The **run history** records each run's source, phase and response id

## Speaker Notes

A routine pairs one trigger with one action and keeps a run history. This slide follows one run from the trigger into that history, the way the demo does.

One trigger: the timer fires once at a timestamp with an explicit UTC offset, the schedule fires on a five-field cron expression at most every five minutes, and `github_issue` fires when an issue is opened or closed. The `triggers` map holds exactly one entry, and its key is only a label.

One action: `invoke_agent_responses_api` calls the agent's Responses endpoint, and `invoke_agent_invocations_api` calls a hosted agent's Invocations endpoint. Routines accept prompt agents and hosted agents, not workflow agents.

Dispatch: waiting for 07:00 is no way to test, so `dispatch` queues a run now and only acknowledges that it was enqueued. A disabled routine can be dispatched when the request carries an explicit payload.

Run history: each run records its source (`timer_delivery`, `schedule_delivery` or `queued_dispatch`), the dispatch id and the response id, which leads to the agent response and its trace. The demo is verified when all three sources show `phase=completed`.
