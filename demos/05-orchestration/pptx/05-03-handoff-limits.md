---
slide: "05-03"
topic: 05-task-decomposition
title: "Handoff collaboration with turn limits"
subtitle: "What the analyst finds decides which specialist comes next"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/05-03-handoff-limits.svg
order: 13
source: demos/05-orchestration/05-task-decomposition/demo-task-decomposition.md
gamma_prompt: >
  Slide titled "Handoff collaboration with turn limits", subtitle "What the analyst finds
  decides which specialist comes next". Left half: four short bullets. Right half: a diagram on
  white. A grey task box (ACME Corp's 10-K, foreign revenue 60 percent) leads to a blue
  sec_filing_analyzer box marked start agent with an amber "turn limit: 2" chip. An arrow
  labelled "foreign revenue above 30 percent" hands off to a blue international_risk_assessor
  box marked terminal specialist with its own "turn limit: 2" chip, which leads down to a green
  "Final message" box with currency_risk_score, hedging_recommendations and exposure_by_market.
  A dashed credit_analyst box with a dashed arrow to the risk assessor is marked "did not speak
  in this run". A grey band at the bottom explains with_autonomous_mode(turn_limits=...). Clean
  minimal style, no drop shadows.
---

# Handoff collaboration with turn limits

## Content

- **No plan**: the SEC filing analyst starts and acts on what it finds
- Foreign revenue above **30 percent** triggers the handoff to the risk assessor
- The risk assessor is a **terminal specialist** with no outgoing handoff
- **Autonomous mode** runs without user input; `turn_limits` caps each agent at two turns

## Speaker Notes

The third strategy has no plan: the next specialist is decided by what an agent just found. The demo analyzes ACME Corp's 10-K with 60 percent foreign revenue.

No plan: `sec_filing_analyzer` starts, extracts the financial metrics and follows its instructions on when to hand off. Every handoff agent is created with `require_per_service_call_history_persistence=True`.

Handoff: with foreign revenue at 60 percent the analyst handed off to `international_risk_assessor`, which sees the earlier conversation. The credit analyst could also hand off to the assessor on foreign currency obligations, but it did not speak in this run.

Terminal specialist: the risk assessor has no outgoing handoff by design, so the run ends with its JSON risk report. The builder logs a warning that this agent cannot hand off further.

Turn limits: `with_autonomous_mode` lets agents continue without waiting for a user message, and `turn_limits` caps each listed agent at two turns. .NET has neither, so the C# run ends when an agent answers without handing off.
