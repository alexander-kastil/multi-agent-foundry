---
slide: "08-02"
topic: 08-ag-ui
title: "Approval as an interrupt"
subtitle: "close_ticket waits for the client's answer before it runs"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/08-02-approval-interrupt.svg
order: 21
source: demos/06-hosting/08-ag-ui/demo-ag-ui.md
gamma_prompt: >
  Slide titled "Approval as an interrupt", subtitle "close_ticket waits for the client's answer before it
  runs". Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram of
  two runs on one thread, the first ending with an interrupt before close_ticket runs, the client's answer
  with the interrupt id in the middle, and the second run returning the tool result, shown as an inline
  content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a
  background or side accent. Use exactly the one image given for this card; do not generate or add any other
  images, icons or logos.
---

# Approval as an interrupt

## Content

- A tool with **`approval_mode="always_require"`** does not run when the model calls it
- The run ends with an **interrupt** in the outcome of `RUN_FINISHED`, and there is no `TOOL_CALL_RESULT`
- The client resumes the thread with the interrupt id and a decision in a **`resume`** entry
- `"approved": false` gives no tool result, and `"status": "cancelled"` abandons the run

## Speaker Notes

This slide follows lab Step 3: the agent is asked to close ticket T-1001, and close_ticket needs approval.

Marked for approval: in Python close_ticket carries approval_mode always_require, and in .NET it is wrapped in ApprovalRequiredAIFunction. The model can call it, but the tool does not run.

The interrupt: run-1 finishes with outcome interrupt and the message "Approve running close_ticket?". There is no TOOL_CALL_RESULT, and nothing happens until the client answers.

Resume: approve.py sends run-2 on the same thread approval-1, with the transcript from MESSAGES_SNAPSHOT and a resume entry that carries the interrupt id, status resolved and approved true. The tool result "Ticket T-1001 closed: Resolved" appears and the agent reports it.

Other answers: with approved false there is no tool result at all, and status cancelled abandons the interrupted run. The C# server pauses the same way, but it sends no MESSAGES_SNAPSHOT, so approve.py cannot resume it.
