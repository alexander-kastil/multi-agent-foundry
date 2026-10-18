---
slide: "02-01"
topic: 02-transports
title: "Server processes per transport"
subtitle: "stdio starts one per client, Streamable HTTP shares one"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/02-mcp/pptx/images/02-01-server-processes.svg
order: 5
source: demos/02-mcp/02-transports/demo-transports.md
gamma_prompt: >
  Slide titled "Server processes per transport", subtitle "stdio starts one per client,
  Streamable HTTP shares one". Left half holds four short bullets on stdio, the standard streams,
  Streamable HTTP and process ids. Right half shows a two-panel diagram: on the stdio side two
  client runs each launch their own amber server process over stdin and stdout with logs on
  stderr; on the Streamable HTTP side two client runs send POST /mcp to one shared amber server on
  port 47004. White background, clean sans-serif font.
---

# Server processes per transport

## Content

- **stdio**: the client launches the server as a child process
- **Standard streams**: stdin and stdout carry JSON-RPC, stderr carries the logs
- **Streamable HTTP**: one server on `127.0.0.1:47004` serves every client
- **Process ids**: new on every stdio run, the same on every HTTP run

## Speaker Notes

This slide shows the difference the lab measures with server_info, a tool that reports the process id and the transport that answered. Both sides run the same server.py from transports-py.

stdio: stdio_client.py does not connect to a running server, it starts server.py with --transport stdio as a child process. Every client gets its own server process.

Standard streams: each message is one line of JSON on standard input and standard output. The server must never print anything else to standard output, so it logs to standard error.

Streamable HTTP: the server runs on its own, and each client sends every message as a POST to /mcp. The server answers each request with one JSON object or an SSE stream.

Process ids: two stdio_client.py runs print two different process ids, and two http_client.py runs print the same one, because the HTTP server outlives its clients. Foundry Agent Service accepts only a remote endpoint, so a stdio server has to be hosted over HTTP first.
