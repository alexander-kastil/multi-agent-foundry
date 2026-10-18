---
slide: "06-02"
topic: 06-agent-lifecycle
title: "Token limits per tenant"
subtitle: "A gateway in front of the agent answers 429 or 403"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/06-02-token-gateway.svg
order: 15
source: demos/08-governance/06-agent-lifecycle/demo-agent-lifecycle.md
gamma_prompt: >
  Slide titled "Token limits per tenant", subtitle "A gateway in front of the agent answers 429 or 403".
  Layout: Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram
  of three tenants, the retrying client, the token-limit gateway with its two checks, the Foundry agent
  and the chargeback as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for
  this card; do not generate or add any other images, icons or logos.
  Theme: Integrations House (themeId idege54tfobdv2k), Montserrat bold headings, Poppins body, #1B365D accent.
---

# Token limits per tenant

## Content

- Every caller of a model deployment shares its tokens-per-minute quota
- Over the **monthly quota**: 403, and the client stops
- Over **tokens per minute**: 429 with `retry_after`, retried with exponential backoff and jitter
- **Chargeback** prices each tenant from `response.usage` plus a share of the compute cost

## Speaker Notes

This slide shows how the lab keeps one busy tenant from starving the others on the same model deployment. The gateway class simulates the `llm-token-limit` policy of Azure API Management and shortens the one-minute window to 6 seconds.

Shared quota: a model deployment has one tokens-per-minute quota for all of its callers. A token limit per tenant in front of the agent stops one tenant from using it up.

Monthly quota: the gateway checks the period quota first, and a breach returns 403 with `token_quota_exceeded`. The client treats a 403 as final.

Tokens per minute: a breach of the window budget returns 429 with `retry_after`, and the client retries with exponential backoff and jitter. In the demo the developer tenant has 150 tokens per window, gets a 429 on its third request, waits and then succeeds.

Chargeback: each tenant is priced from the tokens the Responses API reports in `response.usage`, and the shared compute cost is split evenly. The rates in the code are illustrative, not Azure prices.
