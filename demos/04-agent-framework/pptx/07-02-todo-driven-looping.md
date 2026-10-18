---
slide: "07-02"
topic: 07-harness-agent
title: "Todo-driven looping"
subtitle: "The harness runs the agent again while todos remain open"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/07-02-todo-driven-looping.svg
order: 18
source: demos/04-agent-framework/07-harness-agent/demo-harness-agent.md
gamma_prompt: >
  Slide titled "Todo-driven looping", subtitle "The harness runs the agent again while todos remain open". Two
  columns. Left column: four short bullets. Right column, wider than the left: the provided diagram of a loop
  from an agent run through a todo check and a reminder message back to the agent, with four stage todos and
  a with and without loop comparison, shown as an inline content image at its natural 16:9 aspect ratio,
  scaled to fit the column, never cropped, never used as a background or side accent. Use exactly the one
  image given for this card; do not generate or add any other images, icons or logos.
---

# Todo-driven looping

## Content

- **Recording progress** in todos does not run the agent again
- **loop_should_continue** with `todos_remaining` checks for open todos in `execute` mode
- **loop_next_message** sends a reminder of the open todos and runs the agent again
- **loop_max_iterations** bounds the loop, so always set it

## Speaker Notes

Some models stop after one step. In Step 3 of the demo the agent is told to finish exactly one todo per reply, which makes the loop easy to see.

Recording progress: marking a todo complete does not re-invoke the agent. Without the loop the demo ends with 1 of 4 todos complete.

The check: todos_remaining with looping_modes set to execute decides whether open todos are left. When none are left, the run ends.

The reminder: todos_remaining_message re-invokes the agent with a reminder of the open todos. With the loop the demo reaches 4 of 4 todos complete.

The bound: loop_max_iterations stops the loop after six iterations in the demo, even when todos remain. Looping is experimental in the harness and prints an ExperimentalWarning.
