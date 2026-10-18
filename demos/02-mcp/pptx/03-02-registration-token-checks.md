---
slide: "03-02"
topic: 03-mcp-authorization
title: "From app registration to token checks"
subtitle: "How the Entra settings of m2-mcp-hr-api reach the token and the server"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/03-02-registration-token-checks.svg
order: 8
source: demos/02-mcp/03-mcp-authorization/demo-mcp-authorization.md
gamma_prompt: >
  Slide titled "From app registration to token checks", subtitle "How the Entra settings of
  m2-mcp-hr-api reach the token and the server". Left half holds four short bullets: audience,
  issuer, scopes and claim names. Right half shows a three-column mapping diagram: purple app
  registration settings on the left, blue access token claims aud, iss, scp and azp in the
  middle, and amber server checks such as ValidAudience and ValidIssuer on the right, linked row
  by row with arrows. White background, clean sans-serif font, settings and claims in a
  monospace font.
---

# From app registration to token checks

## Content

- **Audience**: the Application ID URI and v2 tokens make `aud` the API's client ID
- **Issuer**: v2 tokens carry the v2.0 issuer that `ValidIssuer` expects
- **Scopes**: the Azure CLI, pre-authorized for `hr.read`, gets a token with `scp` `hr.read`
- **Claim names**: `MapInboundClaims = false` keeps `scp` readable for the scope check

## Speaker Notes

provision-entra.azcli creates the app registration m2-mcp-hr-api, and each setting it writes shows up again in the decoded token and in Program.cs. Read the three columns from left to right to see why each setting is there.

Audience: Entra compares the requested resource with the Application ID URI and issues a token whose aud is the API's client ID. The JWT bearer handler accepts only that audience, so a token for another API gets 401.

Issuer: requestedAccessTokenVersion 2 turns on v2 access tokens, whose iss is the v2.0 issuer of the tenant. ValidIssuer is set to the same URL through entra.Authority.

Scopes: the registration exposes hr.read and hr.write and pre-authorizes the Azure CLI for hr.read only, so the token arrives with scp hr.read and no consent screen. The azp claim carries the Azure CLI client ID, which stands in for an interactive MCP client in this demo.

Claim names: with the default mapping the handler renames scp to a long URI, and every write call fails with 403 even for a token that has the scope. MapInboundClaims = false keeps the claim named scp for ScopeChallengeMiddleware.
