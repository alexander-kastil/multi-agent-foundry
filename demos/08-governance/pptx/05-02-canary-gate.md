---
slide: "05-02"
topic: 05-cicd-gates
title: "The canary gate on agent versions"
subtitle: "Production stays pinned while the candidate is tested"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/05-02-canary-gate.svg
order: 13
source: demos/08-governance/05-cicd-gates/demo-cicd-gates.md
gamma_prompt: >
  Slide titled "The canary gate on agent versions", subtitle "Production stays pinned while the
  candidate is tested". Layout: Two columns. Left column: the bullets. Right column, wider than the
  left: the provided diagram of the four canary gate steps beside the pinned stable endpoint and the
  pass and fail outcomes as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for
  this card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# The canary gate on agent versions

## Content

- Pin the stable endpoint to the **stable version** before the canary exists
- Both versions answer `eval-set.jsonl`, each named in the **agent reference**
- **accuracy** and `schema_valid_rate` may not drop more than 5 points
- A pass moves the pin to the canary; a failure leaves it on the stable version

## Speaker Notes

This slide follows `canary_gate.py` through its four steps and shows that production traffic stays on version 1 the whole time.

Pin first: the version selector follows the latest version by default, so a pipeline that deploys and then tests would change production traffic before the test runs. The gate therefore pins the endpoint to the stable version before it creates the canary.

Test by reference: both versions answer the same six labeled code samples. Each call names its version in the agent reference, which bypasses the selector.

Two metrics: accuracy against the expected verdict, and the share of replies that are valid JSON. The canary passes when neither metric drops more than 5 points below the stable version.

Promote or keep: Foundry does not split traffic between versions, so promotion is one `pin_endpoint` call that moves all traffic to the canary. In the `regress` run the canary scores 0.0 on both metrics, the pin never moves and the exit code is 1.
