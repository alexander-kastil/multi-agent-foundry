---
slide: "05-02"
topic: 05-mcp-resilience
title: "Outage and recovery in the probe"
subtitle: "Three slow failures open the circuit and two successes close it"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/05-02-outage-timeline.svg
order: 12
source: demos/02-mcp/05-mcp-resilience/demo-mcp-resilience.md
gamma_prompt: >
  Slide titled "Outage and recovery in the probe" with subtitle "Three slow failures open the circuit
  and two successes close it". Left half: four short bullets about slow failing calls, the circuit
  opening, the fast fallback answer and recovery. Right half: the provided timeline diagram of six
  calls with the circuit state band and the tool results below, at full legibility on white. Use
  exactly the one image given for this card; do not generate or add any other images, icons or logos.
---

# Outage and recovery in the probe

## Content

- With the outage on, each failing call makes **3 attempts** and takes about 0.9 s
- After **3 failed calls** the circuit is `open`
- The open circuit answers in milliseconds with `service_circuit_open` and `retry_after_seconds`
- After the **10 s** recovery timeout, a trial call and a second success close it

## Speaker Notes

This slide is Step 3 of the lab: `probe.py` turns on an outage, makes four calls, turns it off and makes two more. Read it left to right while the probe output scrolls.

Slow failures: a failing call takes about 0.9 seconds because it makes three attempts with growing delays. It returns `retries_exhausted` with the fallback `consult_pharmacist`.

The breaker opens: each failed tool call counts once toward the threshold of three, so the third one moves the circuit to `open`. The attempts inside a call do not count separately.

Fast answers: the next call is answered in milliseconds without touching the backend, with `service_circuit_open` and a `retry_after_seconds` hint of 9.8. The fields tell the model what it may and may not conclude.

Recovery: after the recovery timeout of ten seconds, the first call is a trial in `half_open` and the second success closes the circuit. No restart is needed.
