---
slide: "04-02"
topic: 04-durable-agents
title: "Restarting the host during the approval wait"
subtitle: "The instance waits in the scheduler, not in the process"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/04-02-restart-timeline.svg
order: 11
source: demos/06-hosting/04-durable-agents/demo-durable-agents.md
gamma_prompt: >
  Slide titled "Restarting the host during the approval wait", subtitle "The instance
  waits in the scheduler, not in the process". Left half holds four short bullets: a
  rejected draft moves the workflow to review round 2, the host is stopped and started
  again, the status call returns the same round afterwards, and on Flex Consumption the
  app scales to zero during the wait. Right half shows the supplied timeline diagram with
  four lanes (you, function app process, Durable Task Scheduler, status call) where the
  process lane has a gap while the scheduler lane runs on unbroken. Clean minimal style,
  white background, no extra imagery.
---

# Restarting the host during the approval wait

## Content

- After the rejection, the status reads **waiting for review, round 2**
- `Ctrl+C` stops the host and `func start` **brings it back**
- The status call returns **the same round** after the restart
- On **Flex Consumption** the app scales to zero while the record waits

## Speaker Notes

This slide replays Step 4 of the demo on a timeline. The question it answers is where the waiting instance lives while the host is gone.

Reject, then check: the rejection with feedback makes the agent rewrite the draft, and the status call reports waiting for review, round 2. That status comes from the orchestration record, not from the running process.

Stop and start the host: `Ctrl+C` ends the Functions host and `func start` brings it back. The instance was never held in that process.

Same status after the restart: the status endpoint returns waiting for review, round 2 again, and the approval then completes the instance with the status published. The Durable Task Scheduler stores orchestration and entity state and dispatches work to the app.

Flex Consumption: in Azure the function app scales to zero during the wait, so a 24 hour approval costs seconds of execution time. While the instance waits, nothing executes in Azure, and the only thing alive is the orchestration record in the scheduler.
