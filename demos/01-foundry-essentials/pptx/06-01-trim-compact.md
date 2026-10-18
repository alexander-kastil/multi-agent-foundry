---
slide: "06-01"
topic: 06-context-engineering
title: "Trimming and compaction compared"
subtitle: "The same 20-turn history sent whole, trimmed and compacted"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/06-01-trim-compact.svg
order: 16
source: demos/01-foundry-essentials/06-context-engineering/demo-context-engineering.md
gamma_prompt: >
  Slide titled "Trimming and compaction compared", subtitle "The same 20-turn history sent
  whole, trimmed and compacted". Left half holds four short bullets: the full history is
  20 turns, 80 items and 11,322 input tokens and the answer is right; trimming to the last
  4 turns sends 2,287 tokens but loses turn 1, so the model does not know the answer;
  compaction keeps the 40 messages and turns the 40 tool items into one encrypted
  compaction item; the next turn costs 1,259 input tokens and still knows the booking
  reference. Right half shows the supplied diagram with three columns of item blocks,
  token bars and the answers. Clean and light, white background.
---

# Trimming and compaction compared

## Content

- The full history: **20 turns**, 80 items, 11,322 input tokens, and the answer is right
- **Trim by turn** to the last 4: 2,287 input tokens, but turn 1 is gone and the model does not know
- **Compact**: the 40 messages stay, the 40 tool items collapse into one encrypted `compaction` item
- The next turn costs **1,259 input tokens** and still knows booking reference ZX-4417

## Speaker Notes

This slide puts steps 2 and 3 of the demo side by side, because both shrink the same 20-turn weather history. Each turn is a user message, a `get_forecast` function call, its output and an assistant message.

The baseline: all 20 turns are 80 items and 11,322 input tokens, and the model answers with the booking reference and the travel companion from turn 1. Tool results dominate, which is why the hatched blocks are the widest.

Trim by turn: keeping the last 4 turns cuts the request by about 80 percent, and each turn stays whole so no `function_call_output` loses its call. The booking reference was stated in turn 1, so the trimmed request answers "I don't know."

Compact: `responses.compact` keeps the 40 messages verbatim and replaces the 40 tool items with one encrypted `compaction` item. That item is not meant to be read by people.

The result: the next turn costs 1,259 input tokens instead of 11,322 and still answers with ZX-4417 and Marta. The compaction call reads the whole history once and its output size varies between runs, so measure before you rely on a ratio.
