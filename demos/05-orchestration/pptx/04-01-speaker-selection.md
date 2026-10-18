---
slide: "04-01"
topic: 04-magentic
title: "Speaker selection in group chat"
subtitle: "Round robin and an orchestrator agent on the same slogan team"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/04-01-speaker-selection.svg
order: 9
source: demos/05-orchestration/04-magentic/demo-magentic.md
gamma_prompt: >
  Slide titled "Speaker selection in group chat", subtitle "Round robin and an orchestrator
  agent on the same slogan team". Left half: four short bullets comparing a group chat whose
  speaker is picked by a plain Python round robin function with one whose speaker is picked by
  an orchestrator agent. Right half: a two-lane diagram on white. Top lane: an amber box
  "Selection function" followed by four blue turn boxes (Writer, Reviewer with a green APPROVED
  chip, Writer, Reviewer with APPROVED) and a white final box "termination condition reached".
  Bottom lane: an amber box "Agent orchestrator" followed by Writer, Reviewer with APPROVED and
  a green final box "approved slogan", with the caption "Two turns instead of four". Clean
  minimal style, no drop shadows.
---

# Speaker selection in group chat

## Content

- **Selection function**: plain Python maps the round number to the next speaker
- Round robin ran **four turns**, although the reviewer approved in turn 2
- **Agent orchestrator**: a model reads the chat, picks the speaker and can end it
- The orchestrated chat ended after **two turns** with the approved slogan

## Speaker Notes

The demo runs the same writer and reviewer team twice and changes only how the next speaker is chosen. Everyone sees one shared conversation, and after each turn the orchestrator picks who speaks next.

Selection function: `round_robin` maps the current round to a participant, so you own the stopping rule. Here the termination condition stops the chat after four assistant messages.

Four turns: the reviewer approved the first slogan, but the writer went on. The selection function only counts rounds.

Agent orchestrator: with `orchestrator_agent` a model reads the conversation, picks the next speaker and can end the chat. Its instructions tell it to finish as soon as the reviewer says APPROVED.

Two turns: the orchestrated run ends after the writer and the reviewer, and the final output is the approved slogan instead of a termination notice. The termination condition is still there as the safety net.
