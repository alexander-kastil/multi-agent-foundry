---
slide: "04-01"
topic: 04-continuous-evaluation
title: "Evaluating live and stored traffic"
subtitle: "A rule, stored response IDs and traces feed the same evaluators"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/04-01-evaluating-live-traffic.svg
order: 10
source: demos/07-evaluation/04-continuous-evaluation/demo-continuous-evaluation.md
gamma_prompt: >
  Slide titled "Evaluating live and stored traffic", subtitle "A rule, stored response IDs and traces
  feed the same evaluators". One half holds five short bullets: a continuous evaluation rule scores each
  completed response under a max_hourly_runs cap, stored responses are selected by ID through
  azure_ai_responses, traces are selected from Application Insights through azure_ai_traces, all three
  use the same evaluators and evals API as the offline suite, and the project managed identity needs
  Foundry User and Reader roles. The other half shows the supplied diagram: a prompt agent on the left
  with three lanes, each running from what is scored through an amber data source box to a purple
  evaluation run. White background, no extra imagery.
---

# Evaluating live and stored traffic

## Content

- A **continuous evaluation rule** scores each completed response, capped by `max_hourly_runs`
- **Stored responses** are selected by ID through `azure_ai_responses`
- **Traces** are selected from Application Insights through `azure_ai_traces`
- All three use the same evaluators and **`evals` API** as the offline suite
- The project managed identity needs **Foundry User** and Reader roles

## Speaker Notes

A release evaluation proves a version was good on the day it shipped; this slide shows the three ways the demo keeps scoring m7-support-agent afterwards.

Rule: an evaluation lists the evaluators and the rule decides when it runs, here on every RESPONSE_COMPLETED event of one agent. max_hourly_runs caps the cost at 100 runs an hour, and each run holds one response whose scores also appear on the Monitor tab.

Stored responses: a rule only sees what happens after it exists, so earlier traffic is scored by passing response IDs. The IDs go in inline as file_content, and all six passed Violence and Coherence.

Traces: trace evaluation, in preview, reads the invoke_agent spans that Foundry wrote and rebuilds query and response from their gen_ai attributes. The same source scores agents that do not run on Foundry, as long as they emit those spans.

Same API: the modes share the evaluators and the evals API of the offline suite. A rubric you trust before release also runs after it.

Roles: the rule runs under the project managed identity, which needs Foundry User on the project, or rules are created but no results appear. Trace evaluation also needs Reader on Application Insights, plus Log Analytics Reader on the workspace when it requires workspace permissions.
