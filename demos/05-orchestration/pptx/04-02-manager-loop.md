---
slide: "04-02"
topic: 04-magentic
title: "Magentic manager loop"
subtitle: "Task ledger, plan review and a progress ledger every round"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/04-02-manager-loop.svg
order: 10
source: demos/05-orchestration/04-magentic/readme.md
gamma_prompt: >
  Slide titled "Magentic manager loop", subtitle "Task ledger, plan review and a progress
  ledger every round". A narrow text column with four short bullets beside a large diagram on
  white. The diagram shows one task message entering an amber "Task ledger" box (known facts,
  facts to look up, plan for the team) with purple event chips PLAN_CREATED and REPLANNED; a
  grey "Plan review" box below it with approve() and revise(feedback); an amber "Progress
  ledger" box in the centre listing five questions and a PROGRESS_LEDGER_UPDATED chip; blue
  Researcher and Analyst boxes on the right that receive the next speaker and reply; a grey
  "Loop limits" box with max_round_count=8, max_stall_count=2, max_reset_count=1; and a green
  "final answer" box reached through satisfied=True. Clean minimal style, no drop shadows.
---

# Magentic manager loop

## Content

- **Task ledger**: known facts, facts to look up and a plan, announced as `PLAN_CREATED`
- **Plan review** pauses with `request_info`: `approve()` proceeds, `revise()` replans
- **Progress ledger** answers five questions every round and names the next speaker
- Too many stalls reset and replan; **loop limits** bound the whole run

## Speaker Notes

Magentic puts a manager in the middle that plans, tracks and replans, following the Magentic-One design. This slide walks the loop the offsite planning team runs in the demo.

Task ledger: from the one task message the manager writes the known facts, the facts to look up and a plan for the team. The workflow announces it as a `magentic_orchestrator` event of type `PLAN_CREATED`.

Plan review: with `enable_plan_review=True` the workflow pauses with a `request_info` event that carries a `MagenticPlanReviewRequest`. `approve()` proceeds, and `revise("feedback")` makes the manager replan, which arrives as `REPLANNED`.

Progress ledger: every round the manager asks whether the request is satisfied, whether the team is in a loop, whether progress is being made, who speaks next and what they should do. Each round emits `PROGRESS_LEDGER_UPDATED`, and `satisfied=True` ends the loop before the final report.

Loop limits: rounds without progress raise a stall counter, and past `max_stall_count` the manager resets and replans. `max_reset_count` and `max_round_count` bound the whole loop, because without them a confused team keeps spending tokens.
