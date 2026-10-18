---
slide: "04-02"
topic: 04-zero-trust
title: "Tenant from a verified token"
subtitle: "Why the agent cannot read another tenant's review"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/04-02-tenant-from-token.svg
order: 11
source: demos/08-governance/04-zero-trust/demo-zero-trust.md
gamma_prompt: >
  Slide titled "Tenant from a verified token", subtitle "Why the agent cannot read
  another tenant's review". Left third: four short bullets on token verification, the
  tenant context variable, tools without a tenant parameter and partitioned data.
  Right two thirds: a two-row flow diagram; the top row runs from a signed request
  through token verification to the request context, the bottom row from the agent
  through the get_review tool and the repository to the green tenant-a partition, with
  a return arrow labelled null. Clean minimal style, white background, show the whole
  diagram without cropping.
---

# Tenant from a verified token

## Content

- `jwt.decode` checks **signature, audience, issuer and expiry** before any data access
- The verified tenant goes into the **`current_tenant`** context variable
- The tools have **no tenant parameter**, so the model has nothing to spoof
- The repository reads only the **partition of the request's tenant**

## Speaker Notes

In Step 6 a live agent is asked for a review that belongs to tenant B while the request belongs to tenant A. The slide follows that request through the code.

Signature, audience, issuer and expiry: a forged token for `tenant-b` is rejected with `InvalidSignatureError`. A real service validates Microsoft Entra tokens against the published signing keys and checks the `aud` and `tid` claims.

current_tenant: the tenant comes from the verified claims, never from the model or a request field, and stays set for the rest of the request.

No tenant parameter: `get_review` takes only `review_id`, and the model asked for `rev-b-001` exactly as instructed. It still had no way to name a different tenant.

Partition of the request's tenant: the lookup finds nothing in the partition of `tenant-a`, so the tool returns `null`. Isolation holds even if the model follows the user's instruction perfectly.
