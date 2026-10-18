---
slide: "07-03"
topic: 07-harness-agent
title: "History compaction"
subtitle: "Input tokens per question, with and without compaction"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/07-03-history-compaction.svg
order: 19
source: demos/04-agent-framework/07-harness-agent/demo-harness-agent.md
gamma_prompt: >
  Slide titled "History compaction", subtitle "Input tokens per question, with and without compaction". Two
  columns. Left column: four short bullets. Right column, wider than the left: the provided line chart of input
  tokens over six questions, one line rising to 5639 and one falling to 2888 on the last turn, shown as an
  inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as
  a background or side accent. Use exactly the one image given for this card; do not generate or add any other
  images, icons or logos.
---

# History compaction

## Content

- **Without compaction** every turn sends the whole history again: about 940 more tokens per question
- **max_context_window_tokens** and **max_output_tokens** switch compaction on in the harness
- With a 4000 token window the last turn falls from **5639 to 2888** tokens
- One **ContextWindowCompactionStrategy** compacts before each model call and after the run

## Speaker Notes

get_stage_report returns a long hourly report, so six questions fill the context fast. run_compaction.py asks the same six questions twice and prints the input tokens of each turn.

Without compaction: each turn re-sends everything, so the count grows by about 940 tokens per question and reaches 5639. Long tool loops hit the context window and raise cost.

Switching it on: compaction stays off until both max_context_window_tokens and max_output_tokens are set, or you pass a strategy. It also needs store set to False, so the history stays local.

The result: with a 4000 token window the harness compacts when the history approaches the limit, and the last turn falls to 2888 tokens. The warnings about rejected summary messages on stderr do not stop it.

Two phases: the before phase compacts ahead of every model call inside the tool loop, and the after phase compacts persisted history after the run. Override either phase, or turn compaction off with disable_compaction=True.
