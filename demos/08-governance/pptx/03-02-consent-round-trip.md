---
slide: "03-02"
topic: 03-agent-identity
title: "OAuth consent round trip"
subtitle: "The first call asks for consent, the second continues"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/03-02-consent-round-trip.svg
order: 8
source: demos/08-governance/03-agent-identity/demo-agent-identity.md
gamma_prompt: >
  Slide titled "OAuth consent round trip", subtitle "The first call asks for consent,
  the second continues". Left third: four short bullets on the consent request, the
  consent link, the second call and the user's token. Right two thirds: a sequence
  diagram with four lifelines (User, Your application, Foundry Agent Service, MCP
  server), five messages from top to bottom and an amber note on the user lifeline
  where the user signs in and consents. Clean minimal style, white background, show
  the whole diagram without cropping.
---

# OAuth consent round trip

## Content

- The first call returns an **`oauth_consent_request`** instead of a tool result
- Your application shows the **`consent_link`** to the user
- After consent, a second call continues with **`previous_response_id`**
- The tool runs with **the user's token**, and consent is asked once

## Speaker Notes

With an `oauth2` connection each user keeps their own permissions at the tool. The cost is one consent round trip the first time a user needs that tool.

oauth_consent_request: in Step 5 the output items are `mcp_list_tools` and then `oauth_consent_request`, with no tool result.

consent_link: your application shows this link, and the user signs in and consents.

previous_response_id: the second call points at the first response and continues the conversation. It needs a real OAuth app and a person, so the demo guide does not run it.

The user's token: when the second call succeeds, the tool runs with that user's token. Consent is scoped per tool connection, per project and per user, and later calls by the same user skip it.
