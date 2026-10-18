---
slide: "07-03"
topic: 07-enterprise-a2a
title: "Conflict detection and resolution"
subtitle: "Two detection tiers, priority ranks and an audit entry per decision"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/07-03-conflict-resolution.svg
order: 19
source: demos/06-hosting/07-enterprise-a2a/demo-enterprise-a2a.md
gamma_prompt: >
  Slide titled "Conflict detection and resolution", subtitle "Two detection tiers, priority ranks and an
  audit entry per decision". Two columns. Left column: the bullets. Right column, wider than the left: the
  provided diagram with four stages (rule-based tier, semantic tier, priority ranks, audit trail) and two
  cases running through them, the first resolved by rank without a model call, the second judged by a model
  and escalated, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Conflict detection and resolution

## Content

- **Rule-based tier**: `increase` or `buy` against `decrease` or `sell` is a conflict at confidence 1.0, with no model call
- **Semantic tier**: a model judges the prose and returns `CONSISTENT`, `INCONSISTENT` or `COMPLEMENTARY` with a confidence
- **Priority ranks**: the higher rank wins, and equal ranks escalate to `orchestrator-synthesis`
- **Audit trail**: every decision records tenant, agents, confidence, resolution and both outputs

## Speaker Notes

This slide traces the two pairs of outputs from lab Steps 4 and 5 through the conflict policy: notice, decide and leave evidence.

Rule-based tier: the first pair says increase and decrease, so the cheap rule catches it at confidence 1.0. The highlight skips the semantic tier, because no model is called.

Semantic tier: the second pair disagrees only in prose, so a model judges both analyses. The class run returned INCONSISTENT at 0.99, but the verdict can differ on your run, and COMPLEMENTARY would print conflict=False.

Priority ranks: risk ranks 75 and market-analysis 50, so risk-v1-eastus is chosen. The two market analysis agents both rank 50, so that conflict is escalated to an orchestrator synthesis, with a human review flag below confidence 0.5.

Audit trail: each decision is appended with tenant, conflict type, both agents, confidence, resolution, method, chosen agent and a snapshot of both outputs. That trail lets a reviewer reconstruct why one answer overrode another.
