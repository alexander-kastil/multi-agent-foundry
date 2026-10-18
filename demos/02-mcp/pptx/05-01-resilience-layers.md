---
slide: "05-01"
topic: 05-mcp-resilience
title: "Resilience layers in the server"
subtitle: "The breaker wraps the retry, and the tool returns a status"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/05-01-resilience-layers.svg
order: 11
source: demos/02-mcp/05-mcp-resilience/readme.md
gamma_prompt: >
  Slide titled "Resilience layers in the server" with subtitle "The breaker wraps the retry, and the
  tool returns a status". Left half: four short bullets about the circuit breaker around the retry, the
  retry settings, the router and the status result. Right half: the provided diagram of nested layers
  around one MCP tool beside the four result shapes the model receives, at full legibility on white.
  Use exactly the one image given for this card; do not generate or add any other images, icons or
  logos.
---

# Resilience layers in the server

## Content

- The **circuit breaker** wraps the retrying call, so one tool call counts once
- **Retry** covers transient errors only: 3 attempts, 200 ms base delay, jitter
- The **router** picks an instance by p95 latency or weight and skips unhealthy ones
- The tool never raises: every failure becomes a result with a **status**

## Speaker Notes

This slide shows how the patterns of the topic nest inside one MCP tool, outermost first. The same order holds in the Python server and in the C# server with Polly.

The breaker: `circuit.call` wraps `call_backend`, so it counts one failure per tool call after the retries are used up. Only transient errors count, so a stream of bad medication codes cannot open it.

Retry: the `tenacity` decorator retries only `TransientError`, waits longer after each attempt and adds jitter. With three attempts and a 200 ms base delay, a failing call takes about 0.8 seconds.

Routing: the router prefers the lowest 95th percentile latency, can shift a weighted share of traffic to the canary, and skips instances marked unhealthy. In the demo `drugs-canary` starts unhealthy.

The result: a permanent error returns `invalid_input` without a retry, and exhausted retries or an open circuit return `unavailable` with the fallback `consult_pharmacist`. In C#, `AddCircuitBreaker` comes before `AddRetry` because the first strategy added is the outermost.
