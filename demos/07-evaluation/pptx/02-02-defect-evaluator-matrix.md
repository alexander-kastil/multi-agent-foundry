---
slide: "02-02"
topic: 02-agent-evaluators
title: "Defects and the evaluators that catch them"
subtitle: "Eight evaluators over seven recorded refund runs in the cloud"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/02-02-defect-evaluator-matrix.svg
order: 6
source: demos/07-evaluation/02-agent-evaluators/demo-agent-evaluators.md
gamma_prompt: >
  Slide titled "Defects and the evaluators that catch them", subtitle "Eight evaluators over seven
  recorded refund runs in the cloud". A narrow text column holds five short bullets: Tool Selection
  passes r2 and fails r3, Tool Call Success fails only r4, Task Completion passes r3 and r6 so it should
  not gate alone, Task Adherence fails every defective run, and tool evaluators report n/a for r6. The
  wider side shows the supplied diagram: a matrix of seven runs (r1 correct to r7 wrong_intent) against
  three system and five process evaluators, with green pass, red fail and grey n/a cells and an amber
  outline on the column under discussion. White background, no extra imagery.
---

# Defects and the evaluators that catch them

## Content

- **Tool Selection** passes r2 (right tools, wrong values) and fails r3 (wrong tool)
- **Tool Call Success** fails only r4, where the lookup timed out
- **Task Completion** passes r3 and r6, so it should not gate on its own
- **Task Adherence** fails every defective run
- Tool evaluators report **n/a** for r6, where there is no call to judge

## Speaker Notes

The demo records seven runs of a refund agent, one correct and six with a known defect, and scores them with all eight evaluators in the cloud. Read the matrix by column, not by row.

Tool Selection: it passes r2 because the right tools were called with wrong values, and fails r3 because the wrong tool was called. In the same run, Tool Input Accuracy and Tool Call Accuracy fail r2.

Tool Call Success: it fails only r4, where lookup_order returned a service unavailable timeout. Every other run made calls that ran without technical errors.

Task Completion: it is lenient toward r3 and r6 because their final answers read as finished work. That is why it should not gate on its own.

Task Adherence: it fails every defective run, and its reason names the problem, for r3 an unsupported claim and an unrequested action. A verdict without its reason is a number you cannot act on.

n/a for r6: tool evaluators that find no tool call report n/a instead of failing the run, so only Task Adherence catches the missing lookup. The run prints failed=6 because a trajectory fails when any evaluator fails it, and the cells vary slightly between runs.
