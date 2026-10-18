---
slide: "02-02"
topic: 02-model-router
title: "Routing modes and a model subset"
subtitle: "The four router deployments of the demo"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/02-02-routing-modes.svg
order: 7
source: demos/01-foundry-essentials/02-model-router/demo-model-router.md
gamma_prompt: >
  Slide titled "Routing modes and a model subset", subtitle "The four router deployments
  of the demo". Left half holds five short bullets: Balanced is the default and picks
  the most cost-effective model within a small quality range of the best one; Quality
  picks the highest-rated model and ignores cost; Cost accepts a larger quality band and
  picks the cheapest model in it; a model subset restricts the router to the listed
  models, which double as the fallback set; mode and subset are deployment settings.
  Right half shows the supplied diagram of four panels, one per deployment, each plotting
  the same pool by cost and quality with the picked model highlighted. Clean and light,
  white background.
---

# Routing modes and a model subset

## Content

- **Balanced** is the default: within a small quality range of the best model, the most cost-effective one wins
- **Quality** picks the highest-rated model and ignores cost
- **Cost** accepts a larger quality band and picks the cheapest model in it
- A **model subset** restricts the router to the listed models, which double as the fallback set
- Mode and subset are **deployment settings**, set with a `routing` block

## Speaker Notes

The demo deploys four routers from the same model-router 2025-11-18 and changes only the routing settings. The dots sketch one pool by cost and quality, and the amber dot is the model each mode would pick.

Balanced: m1-router-balanced has no routing block, so it runs the default. It considers models within a small quality range of the best one and picks the most cost-effective.

Quality: m1-router-quality picks the highest-rated model for the prompt and ignores cost. Use it for complex reasoning and critical output.

Cost: m1-router-cost accepts a larger quality band and picks the cheapest model in it. In the demo run its choice for the reasoning prompt spent about eight times the tokens of the others, so cheaper per token is not always cheaper per task.

Subset: m1-router-subset may only choose between gpt-5-mini and gpt-5.4. The router picked gpt-5-mini for all three prompts, and gpt-5.4 stayed available as the failover target.

Deployment settings: you set the mode on the deployment with a routing block, not on the request, and a change can take up to five minutes to apply. That is why the project lists the same model and version for all four deployments.
