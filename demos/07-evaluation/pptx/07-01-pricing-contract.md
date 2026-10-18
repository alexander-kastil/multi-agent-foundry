---
slide: "07-01"
topic: 07-incident-response
title: "The pricing and payment contract"
subtitle: "One prompt change broke the number format the payment step accepts"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/07-01-pricing-contract.svg
order: 17
source: demos/07-evaluation/07-incident-response/demo-incident-response.md
gamma_prompt: >
  Slide titled "The pricing and payment contract", subtitle "One prompt change broke the number format
  the payment step accepts". Left: five short bullets. Right, wider: the provided diagram of a checkout
  pipeline, an order lookup box feeding two purple pricing agent versions that both lead into one
  payment step, which ends in a green paid box and a red rejected box, shown whole and not cropped. Use
  exactly the one image given for this card; do not generate or add any other images, icons or logos.
---

# The pricing and payment contract

## Content

- Each checkout runs **order lookup**, the pricing agent and payment
- Prompt **v2** adds a currency symbol and a sentence of explanation
- process_payment accepts only a **plain number**
- The pricing step reports ok in **both versions**
- 12 checkouts, 6 failed: error rate 50%, **severity P1**

## Speaker Notes

The lab's incident starts with one prompt update, and this slide shows why it broke checkout while every pricing step looked fine.

Pipeline: each of the 12 checkouts runs order lookup, the pricing agent m7-pricing-agent and the payment step. The program sends six checkouts through each prompt version and records every deployment in a change log.

Prompt v2: v1 replies with the numeric value only, with two decimals and no currency symbol. v2 adds the currency symbol and a one sentence explanation, which is friendlier and breaks process_payment.

Contract: process_payment accepts a price only when it fully matches PRICE_PATTERN, a plain number with up to two decimals. Anything else is rejected with non_numeric_amount.

No crash: the agent_pricing step reports ok in both versions, so the pricing trace alone does not show the problem. Nothing crashed; the output only had the wrong format.

Severity: all six v1 checkouts were paid and all six v2 checkouts were rejected, a 50% error rate. Above 5% the severity rule says P1, and that rule was written before the incident so nobody argues about it during one.
