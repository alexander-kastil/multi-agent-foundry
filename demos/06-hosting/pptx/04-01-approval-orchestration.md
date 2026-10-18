---
slide: "04-01"
topic: 04-durable-agents
title: "Announcement approval orchestration"
subtitle: "Draft, review and wait, then publish or rewrite for up to three rounds"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/04-01-approval-orchestration.svg
order: 10
source: demos/06-hosting/04-durable-agents/demo-durable-agents.md
gamma_prompt: >
  Slide titled "Announcement approval orchestration", subtitle "Draft, review and wait,
  then publish or rewrite for up to three rounds". Left half holds four short bullets on
  how a durable orchestration drafts a release announcement with an agent, asks a human
  reviewer, waits for an approval event or a 72 hour timer without running code, and
  rewrites the draft in the same agent session after a rejection. Right half shows the
  supplied flow diagram: start, draft, request review, a wait box with an event and a
  timer, three outcomes (rewrite, publish, timeout) and a durable entity below that holds
  the writer session. Clean minimal style, white background, no extra imagery.
---

# Announcement approval orchestration

## Content

- The orchestration `announcement_approval` **drafts**, requests a review, then waits
- The wait races the `ApprovalDecision` event against a **72 hour timer**, with no code running
- A rejection rewrites the draft **in the same session**, for up to three rounds
- Both agent calls go to the **durable entity** `dafx-AnnouncementWriter`

## Speaker Notes

This is the workflow the demo builds: a writer agent drafts a release announcement, and a human approves it before it goes out. The orchestration sequences the steps, and every step is a checkpoint the framework can replay from.

Draft, then request a review: the orchestration asks the agent for a typed `Announcement`, then the `request_review` activity notifies the reviewer and sets the status to waiting for review. Side effects live in activities, so the orchestration itself stays deterministic.

Event or timer: the orchestration waits for the `ApprovalDecision` event or a 72 hour timer, whichever comes first. While it waits, no code runs and nothing holds the model open.

Rewrite in the same session: a rejection with feedback sends the agent back to rewrite, and because it reuses the session it remembers its first draft. The loop allows three rounds, and a timer that fires first raises `TimeoutError`.

The durable entity: each agent session is a durable entity named `dafx-<agent name>`. It receives the message, calls the model, appends both sides to its stored history and replies.
