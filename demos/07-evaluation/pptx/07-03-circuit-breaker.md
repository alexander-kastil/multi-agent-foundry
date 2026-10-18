---
slide: "07-03"
topic: 07-incident-response
title: "Circuit breaker states"
subtitle: "How the payment step falls back to the backup gateway and recovers"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/07-03-circuit-breaker.svg
order: 19
source: demos/07-evaluation/07-incident-response/demo-incident-response.md
gamma_prompt: >
  Slide titled "Circuit breaker states", subtitle "How the payment step falls back to the backup
  gateway and recovers". Left: four short bullets. Right, wider: the provided state diagram with a
  green closed state, an amber open state and a grey half open state connected by labelled arrows, and
  the primary and backup gateways above, shown whole and not cropped. Use exactly the one image given
  for this card; do not generate or add any other images, icons or logos.
---

# Circuit breaker states

## Content

- Charges go to the primary gateway while the breaker is **closed**
- It **opens** when more than 40% of at least 10 calls fail, and the backup serves charges
- After the probe interval, one **probe call** goes to the primary
- A successful probe **closes** the breaker; a failed one opens it again

## Speaker Notes

Remediation has to be bounded or it becomes the second incident, and the circuit breaker is the lab's bounded fallback for the payment gateway.

Closed: in the normal state every charge goes to the primary gateway, and the breaker records whether each call succeeded.

Open: once more than 40% of at least ten recent calls fail, the breaker opens and serves charges from the backup. In the lab run the last three charges went to the backup.

Probe: after the probe interval, 30 seconds by default and half a second in the demo, one call goes to the primary. The demo prints "after probe: primary, state closed".

Recovery: a successful probe closes the breaker and a failed one opens it again. The gateway runbook applies the same idea to a multi-step switch: idempotent, limited to three runs an hour and escalating when both gateways are unhealthy.
