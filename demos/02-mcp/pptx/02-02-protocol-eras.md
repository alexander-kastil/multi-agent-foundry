---
slide: "02-02"
topic: 02-transports
title: "Two protocol eras on the wire"
subtitle: "A session handshake in 2025-11-25, self-contained requests in 2026-07-28"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/02-02-protocol-eras.svg
order: 6
source: demos/02-mcp/02-transports/demo-transports.md
gamma_prompt: >
  Slide titled "Two protocol eras on the wire", subtitle "A session handshake in 2025-11-25,
  self-contained requests in 2026-07-28". Left half holds four short bullets on the old era, the
  new era, the routing headers and SDK support for both. Right half shows two sequence diagrams
  side by side between wire.py and the transport-lab server: the 2025-11-25 side with initialize,
  a session id highlighted in amber and repeated on every later request, the 2026-07-28 side with
  one tools/call carrying its version headers and a single JSON answer. White background, clean
  sans-serif font, headers in a monospace font.
---

# Two protocol eras on the wire

## Content

- **2025-11-25**: `initialize` returns an `Mcp-Session-Id` that every later request repeats
- **2026-07-28**: no handshake and no session, one `POST` per call
- **Routing headers**: `MCP-Protocol-Version`, `Mcp-Method` and `Mcp-Name` mirror the body
- **Both eras**: the C# and Python SDKs answer both on the same endpoint

## Speaker Notes

wire.py sends raw JSON-RPC to the same server in both protocol eras, so you can read what changed on the wire. Both calls add 2 and 3 and get 5 back.

2025-11-25: the client sends initialize, the server returns an Mcp-Session-Id header, and every later request repeats it. The answers arrive as SSE events with the content type text/event-stream.

2026-07-28: this revision removed the handshake and the session, and every request carries its protocol version and client capabilities in _meta. The server answers a single JSON object, and wire.py prints session: None.

Routing headers: the headers mirror body fields so gateways and load balancers can route without parsing JSON. Because no request depends on an earlier one, a load balancer can send each request to any instance.

Both eras: ModelContextProtocol 2.2.0 and mcp 2.3.0 answer a legacy initialize and a modern per-request call on the same endpoint. The Inspector option --protocol-era forces either one, and both return 5.
