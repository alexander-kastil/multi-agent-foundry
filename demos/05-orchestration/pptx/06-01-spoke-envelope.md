---
slide: "06-01"
topic: 06-resilient-orchestration
title: "Spoke calls with a status envelope"
subtitle: "invoke_spoke returns a status instead of raising"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/06-01-spoke-envelope.svg
order: 14
source: demos/05-orchestration/06-resilient-orchestration/readme.md
gamma_prompt: >
  Slide titled "Spoke calls with a status envelope", subtitle "invoke_spoke returns a status instead of
  raising". Two columns. Left column: the four bullets. Right column, wider than the left: the provided
  diagram of a hub agent calling four spoke agents through invoke_spoke, whose circuit check, concurrency
  limit and timeout each lead to a status chip (circuit_open, timeout, error, success) that feeds one JSON
  envelope back to the hub, shown as an inline content image at its natural 16:9 aspect ratio, scaled to
  fit the column, never cropped, never used as a background or side accent. Use exactly the one image
  given for this card; do not generate or add any other images, icons or logos.
---

# Spoke calls with a status envelope

## Content

- `invoke_spoke` is the **single door** from the hub to the four spoke agents
- A circuit check, a semaphore for two calls and `asyncio.wait_for` guard every call
- Every outcome comes back as a **status**: `success`, `timeout`, `error` or `circuit_open`
- The hub reads the status, names what is missing and continues with what it has

## Speaker Notes

A multi-agent hub fails more often than a single agent, because every spoke is another model call that can time out, throttle or return something unusable. This slide shows the one function every spoke call goes through.

The single door: each hub tool is a thin wrapper that calls `invoke_spoke` with the spoke's name. The specialists stay small, and the resilience code lives in one place.

Three guards: the circuit check skips a spoke known to be failing, the semaphore allows two spoke calls at a time, and `asyncio.wait_for` bounds every call, by default at 90 seconds. In C# a `CancellationTokenSource` and a `SemaphoreSlim` do the same jobs.

Status instead of exceptions: `invoke_spoke` never throws into the hub. It always returns an envelope with `spoke`, `status` and `result`.

The payoff: the hub instructions say what to do with a status other than success, so the model can reason about the gap. In the demo the report says compliance is missing instead of pretending it passed.
