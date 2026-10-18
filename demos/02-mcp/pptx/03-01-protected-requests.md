---
slide: "03-01"
topic: 03-mcp-authorization
title: "Requests through the protected server"
subtitle: "Five demo requests and the stage of mcp-auth-cs that answers each"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/03-01-protected-requests.svg
order: 7
source: demos/02-mcp/03-mcp-authorization/demo-mcp-authorization.md
gamma_prompt: >
  Slide titled "Requests through the protected server", subtitle "Five demo requests and the
  stage of mcp-auth-cs that answers each". Left half holds four short bullets: no token, other
  audience, read token and step-up. Right half shows a swimlane diagram with four amber stages,
  JWT bearer, RequireAuthorization, ScopeChallengeMiddleware and MapMcp, and five blue request
  cards on the left whose arrows stop at the stage that answers them: two 401 badges, one 403
  badge and two green 200 badges. White background, clean sans-serif font.
---

# Requests through the protected server

## Content

- **No token**: `401` with a `resource_metadata` pointer
- **Other audience**: a valid Entra token for Azure Resource Manager also gets `401`
- **Read token**: `list_employees` runs, `add_employee` gets `403 insufficient_scope`
- **Step-up**: with `hr.write` the same call returns `Added Ida Berger`

## Speaker Notes

This slide maps steps 2 to 4 of the lab onto the ASP.NET Core server on port 47010. Each request travels from left to right until one stage answers it.

No token: the endpoint requires authorization, and AddMcp answers 401 with a resource_metadata pointer in WWW-Authenticate. That pointer leads the client to the Protected Resource Metadata.

Other audience: the ARM token is a valid Entra token, but its audience is not this server, so the JWT bearer handler rejects it. This is the check that stops token passthrough between services.

Read token: the hr.read token lists employees, but add_employee is a write tool. ScopeChallengeMiddleware reads the tool name from the tools/call body and answers 403 with the missing scope.

Step-up: after grant-write.azcli, a token for hr.write adds Ida Berger. An interactive client would instead re-authorize with the union of its scopes and hr.write, which shows a consent screen.
