---
slide: "02-01"
topic: 02-model-router
title: "How model router serves a call"
subtitle: "One deployment name in, the serving model in response.model"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/02-01-router-request-flow.svg
order: 6
source: demos/01-foundry-essentials/02-model-router/readme.md
gamma_prompt: >
  Slide titled "How model router serves a call", subtitle "One deployment name in, the
  serving model in response.model". Left half holds five short bullets: the router is
  called like any model with its deployment name; model router 2025-11-18 reads each
  prompt and forwards it to the most suitable model in its pool; the pool spans OpenAI,
  Anthropic, xAI, DeepSeek, Meta and Fireworks, and Claude models are deployed first;
  automatic failover moves a request to the next most suitable model; response.model
  names the serving model. Right half shows the supplied flow diagram from the code
  through the router to the pool and back to response.model. Clean and light, white
  background.
---

# How model router serves a call

## Content

- You call the router like any model: the **deployment name** goes into `model`
- Model router **2025-11-18** reads each prompt and forwards it to the most suitable model in its pool
- The pool spans OpenAI, Anthropic, xAI, DeepSeek, Meta and Fireworks; **Claude models** are deployed first
- **Automatic failover** sends a request to the next most suitable model in the same routing mode
- `response.model` names the model that served the call

## Speaker Notes

Model router is a trained language model that you deploy like any other Foundry model. This slide follows one call through it.

Deployment name: your code never sends a catalog model name, it sends the deployment name, here m1-router-balanced. The call looks exactly like a call to a normal model.

Routing: the router reads each prompt in real time and forwards it to the most suitable model from its pool. Microsoft adds new models to version 2025-11-18 without changing the version number.

The pool: you do not deploy the pool models and you pay what the underlying model charges. Claude models are the one exception: deploy each one on the same Foundry resource first, or a subset that names it fails with InvalidResourceProperties.

Failover: when the chosen model has a transient problem, the router redirects the request to the next most suitable model and keeps your routing mode. It is on by default.

Serving model: response.model carries the underlying model's name, for example gpt-5.6-sol-2026-07-09. A different prompt, or the same prompt a moment later, can return a different name.
