---
slide: "01-01"
topic: 01-workflows
title: "Supersteps in a fan-out run"
subtitle: "Three one-second checks finish in one second"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/01-01-supersteps-fan-out.svg
order: 3
source: demos/05-orchestration/01-workflows/demo-workflows.md
gamma_prompt: >
  Slide titled "Supersteps in a fan-out run", subtitle "Three one-second checks finish in one second".
  Left half holds four short bullets about the superstep scheduler. Right half shows the supplied
  diagram uncropped: three columns for supersteps 1 to 3 separated by dashed amber barrier lines,
  parse_invoice fanning out to duplicate_check, vendor_check and budget_check, which fan in to
  aggregate and its output, above a green band that reads total 1.0s for three one-second checks.
  White background, clean minimal style.
---

# Supersteps in a fan-out run

## Content

- Each **superstep** routes every pending message, then runs all target executors at the same time
- A **barrier** waits for all of them before the next superstep starts
- `add_fan_out_edges` puts the duplicate, vendor and budget checks into one superstep: 1.0s instead of 3.0s
- `add_fan_in_edges` hands `aggregate` one list, and it runs once, after the barrier

## Speaker Notes

This slide follows the routing graph and shows when each executor actually runs. The fan-out demo makes the scheduler visible because every check sleeps for one second.

Supersteps: in each superstep the runtime routes every pending message and runs all target executors concurrently. Streaming the run prints `superstep_started` and `executor_invoked` events, so students can watch the grouping.

The barrier: the workflow waits until every executor in a superstep finishes before the next one starts. A slow branch therefore holds back a fast branch that runs beside it; fold each path into one executor when the paths must progress independently.

Fan-out: the three checks share superstep 2, so three one-second checks cost one second. The script prints a total of 1.0s, not 3.0s.

Fan-in: `aggregate` starts in superstep 3, after the barrier, and is called once with all three results as a list. In the demo the budget check fails, so the output is `failed: budget`.
