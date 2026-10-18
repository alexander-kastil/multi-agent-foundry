---
slide: "06-02"
topic: 06-resilient-orchestration
title: "Circuit breaker"
subtitle: "After three timeouts the next call returns at once"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/06-02-circuit-breaker.svg
order: 15
source: demos/05-orchestration/06-resilient-orchestration/demo-resilient-orchestration.md
gamma_prompt: >
  Slide titled "Circuit breaker", subtitle "After three timeouts the next call returns at once". Two
  columns. Left column: the four bullets. Right column, wider than the left: the provided diagram of four
  calls to the compliance_check spoke in rows, three passing a closed breaker and timing out with
  failures 1 to 3, the fourth stopped by an open breaker with circuit_open, above three grey cards for the
  threshold, the cooldown and the reset, shown as an inline content image at its natural 16:9 aspect
  ratio, scaled to fit the column, never cropped, never used as a background or side accent. Use exactly
  the one image given for this card; do not generate or add any other images, icons or logos.
---

# Circuit breaker

## Content

- The breaker counts **consecutive failures** per spoke
- At `FAILURE_THRESHOLD = 3` it opens, and calls get `circuit_open` back at once
- It stays open for `COOLDOWN_SECONDS = 60`, then the next call goes through
- A success resets the counter to 0

## Speaker Notes

The circuit stage of the demo calls the compliance spoke four times with a 10 millisecond timeout, so every call that reaches the model times out. The diagram shows those four calls row by row.

Counting failures: each timeout or error adds one to the spoke's counter. Calls 1 to 3 print `timeout` with `failures=1`, `failures=2` and `failures=3`.

Opening: at the third failure the breaker records when it opened, and call 4 gets `circuit_open` without touching the model. The failure count stays at 3.

Cooldown: for 60 seconds every call fails fast, which protects a degraded spoke from retry storms and keeps latency down. After that the next call is allowed through.

Reset: a success sets the counter back to 0. The state lives in a dictionary inside the process, so several workers would need a shared store such as a cache.
