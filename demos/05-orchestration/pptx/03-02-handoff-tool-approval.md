---
slide: "03-02"
topic: 03-orchestration-patterns
title: "Handoff with a tool approval"
subtitle: "The driver loop answers every request the workflow raises"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/03-02-handoff-tool-approval.svg
order: 8
source: demos/05-orchestration/03-orchestration-patterns/demo-orchestration-patterns.md
gamma_prompt: >
  Slide titled "Handoff with a tool approval", subtitle "The driver loop answers every request the
  workflow raises". Left half holds four short bullets about the handoff run. Right half shows the
  supplied sequence diagram uncropped: lifelines for triage_agent, the handoff.py driver loop,
  billing_agent and the billing tools, a handoff from triage to billing, a free lookup_charges call,
  and three amber request_info pauses answered by the driver: a tool approval for issue_refund and
  two user replies, the last one ending with terminate(). White background, clean minimal style.
---

# Handoff with a tool approval

## Content

- `triage_agent` hands the duplicate-charge question to **billing_agent**
- `lookup_charges` is `never_require` and runs without a pause
- `issue_refund` is `always_require`: the workflow pauses for a **function approval**
- A **HandoffAgentUserRequest** asks for the user's reply; `terminate()` ends the conversation

## Speaker Notes

This slide traces one run of `handoff.py`. Every pause in the workflow is a `request_info` event, and the driver loop answers each one.

The handoff: the triage agent receives the customer message and hands it to billing, and the script prints `[handoff] triage_agent -> billing_agent`. From here on, billing owns the conversation.

Free tool: `lookup_charges` is declared with `approval_mode="never_require"`, so billing calls it without a pause. It reports two charges of 29.00 EUR, one of them a duplicate.

Approval: `issue_refund` is `always_require`, so the workflow pauses with a function approval request before the refund executes. The driver answers with `to_function_approval_response(approved=True)`; with `APPROVE_REFUNDS = False` the refund does not run.

User replies: when billing answers without handing off, the workflow pauses with a `HandoffAgentUserRequest`. The driver replies with `create_response` and, once its scripted replies run out, ends the conversation with `terminate()`.
