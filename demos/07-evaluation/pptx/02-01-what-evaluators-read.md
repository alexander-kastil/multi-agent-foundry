---
slide: "02-01"
topic: 02-agent-evaluators
title: "What agent evaluators read"
subtitle: "System evaluation judges the whole run, process evaluation the tool calls"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/02-01-what-evaluators-read.svg
order: 5
source: demos/07-evaluation/02-agent-evaluators/readme.md
gamma_prompt: >
  Slide titled "What agent evaluators read", subtitle "System evaluation judges the whole run, process
  evaluation the tool calls". One half holds five short bullets: agent evaluators read the trajectory,
  query holds the system and user messages while response holds the tool calls, results and answer,
  tool evaluators add tool_definitions, system evaluation judges the outcome of the whole run, and
  process evaluation checks the steps. The other half shows the supplied diagram: a refund trajectory
  as six message cards grouped under query, response and tool_definitions, with the two tool cards in
  amber, and below it a purple row of three system evaluators and an amber row of five process
  evaluators. White background, no extra imagery.
---

# What agent evaluators read

## Content

- Agent evaluators read the **trajectory**, not only the final text
- `query` holds the system and user messages, `response` the tool calls, results and answer
- Tool evaluators add **`tool_definitions`**, the schemas the agent could call
- **System evaluation** judges the outcome of the whole run
- **Process evaluation** checks the steps, which for an agent mostly means tool calls

## Speaker Notes

A final answer that reads well can hide a broken run, so this slide opens up what an agent evaluator actually gets to see.

Trajectory: Foundry reads message arrays that carry system instructions, tool calls and tool results, plus the schemas of the tools. The example is the refund request for order A100 that the demo builds on.

Query and response: each evaluator needs a different subset of the row. Tool Call Success needs only response, while Task Adherence needs query and response.

Tool definitions: the tool evaluators add tool_definitions, the schemas of the tools the agent could use. A row can also use messages instead of query and response.

System evaluation: Task Completion, Task Adherence and Intent Resolution judge the outcome of the whole run. Task Navigation Efficiency and Customer Satisfaction belong to the same group but are not part of the demo.

Process evaluation: Tool Call Accuracy, Tool Selection, Tool Input Accuracy, Tool Output Utilization and Tool Call Success check the tool calls. A run with a perfect tool call that answers the wrong question passes Tool Call Accuracy and fails Intent Resolution.
