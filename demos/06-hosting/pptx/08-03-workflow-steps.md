---
slide: "08-03"
topic: 08-ag-ui
title: "Workflow step events"
subtitle: "STEP_STARTED and STEP_FINISHED for every executor"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/08-03-workflow-steps.svg
order: 22
source: demos/06-hosting/08-ag-ui/demo-ag-ui.md
gamma_prompt: >
  Slide titled "Workflow step events", subtitle "STEP_STARTED and STEP_FINISHED for every executor". Two
  columns. Left column: the bullets. Right column, wider than the left: the provided diagram of two nested
  supersteps, one for the triage executor and one for the reply executor, each opened and closed by step
  events, above a two-segment progress indicator and a note on the C# server, shown as an inline content
  image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background
  or side accent. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Workflow step events

## Content

- `AgentFrameworkWorkflow` serves the ticket workflow on `/workflow`, with a new instance per thread
- Each superstep and each executor, **triage** then **reply**, opens and closes with step events
- The step events are enough to draw a **progress indicator** of which agent is working
- The C# `AsAIAgent()` route streams `TEXT_MESSAGE_*` only, authored by triage and reply

## Speaker Notes

This slide shows the events lab Step 4 prints for the two-executor workflow on the Python server.

Workflow as an endpoint: AgentFrameworkWorkflow adapts the workflow to AG-UI, and its factory gives each thread its own workflow instance.

Nested steps: superstep 1 wraps the triage executor and superstep 2 wraps reply, each opened by STEP_STARTED and closed by STEP_FINISHED. Triage classifies the ticket and gives a priority, and reply writes the two-sentence customer reply.

Progress: STEP_STARTED triage marks triage as working and STEP_FINISHED marks it done. The animation fills each segment while its superstep runs.

The C# server: BuildSequential(triage, reply).AsAIAgent() streams text and tool calls, but it does not currently emit step events, activity snapshots or workflow interrupts. Its run contains TEXT_MESSAGE events only, with triage and reply as the authors.
