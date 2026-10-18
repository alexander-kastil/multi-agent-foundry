---
slide: "01-01"
topic: 01-hosted-agents
title: "Session sandbox lifecycle"
subtitle: "What runs once per version and what runs per session"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/01-01-sandbox-lifecycle.svg
order: 3
source: demos/06-hosting/01-hosted-agents/readme.md
gamma_prompt: >
  Slide titled "Session sandbox lifecycle", subtitle "What runs once per version and what runs per session".
  The left half holds four short bullets on how Foundry Agent Service runs a hosted agent: each new version
  pulls the image or builds the ZIP, creates the agent identity and publishes the endpoint; every session gets
  its own VM-isolated sandbox; an idle timeout of 2 to 60 minutes deprovisions it; a returning session gets its
  sandbox back. The right half shows the supplied diagram unchanged: a top row of three purple steps done once
  per version and a lower row of three amber session states (new session, sandbox running, deprovisioned) with
  a return arrow for a session that comes back. White background, clean sans-serif type, no extra imagery.
---

# Session sandbox lifecycle

## Content

- Each new version pulls the image or builds the ZIP, creates the **agent identity** and publishes the endpoint
- Every session gets its own **VM-isolated sandbox** with a persistent `$HOME` and `/files`
- After an **idle timeout** of 2 to 60 minutes (15 by default) the platform deprovisions the sandbox
- When the same session returns, the platform **restores** its sandbox

## Speaker Notes

This slide separates the work Agent Service does once per version from the work it repeats for every session. That split explains why the first call after a quiet period is slower.

Agent identity: creating a version makes Agent Service pull your image or build your ZIP, create a dedicated Entra identity and publish a dedicated endpoint. Each version is immutable, and an endpoint serves one version at a time.

VM-isolated sandbox: every session gets its own sandbox with a persistent `$HOME` and `/files`, sized at 0.5 vCPU and 1 GiB, 1 vCPU and 2 GiB, or 2 vCPU and 4 GiB.

Idle timeout: compute is not kept warm, so the platform deprovisions the sandbox after an idle timeout of 2 to 60 minutes, 15 by default.

Restores: when the same session returns, the platform restores its sandbox. Step 5 of the demo shows the cost: the first call after a quiet period waits while the platform provisions the session sandbox.
