---
slide: "01-02"
topic: 01-tracing
title: "Spans and logs in Application Insights"
subtitle: "Three tables joined on one operation_Id"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/01-02-spans-logs-tables.svg
order: 4
source: demos/07-evaluation/01-tracing/readme.md
gamma_prompt: >
  Slide titled "Spans and logs in Application Insights", subtitle "Three tables joined on one
  operation_Id". One half holds five short bullets: the Azure Monitor distro exports spans and Python
  log records, sampling_ratio=1.0 keeps every span of a short run, client and internal spans go to the
  dependencies table and server spans to requests, log records go to traces with the active trace as
  operation_Id, and a log line carrying trace_id joins its spans on one key. The other half shows the
  supplied diagram: a blue "Your process" panel with three span and log cards feeding the Azure Monitor
  distro, arrows into three Application Insights tables, and an amber band naming the shared key. White
  background, no extra imagery.
---

# Spans and logs in Application Insights

## Content

- The **Azure Monitor distro** exports spans and Python log records from your process
- Pass `sampling_ratio=1.0`, or a short run loses most of its spans
- Client and internal spans go to **`dependencies`**, server spans to **`requests`**
- Log records go to **`traces`**, with the active trace as `operation_Id`
- A log line that carries `trace_id` joins its spans on **one key**

## Speaker Notes

Once a trace crosses services, the next question is where its pieces are stored and how one key brings them back together.

The distro: configure_azure_monitor takes the connection string the project already knows, so no secret sits in your code. With an Agent Framework client, client.configure_azure_monitor reads the same connection string for you.

Sampling: the distro samples by rate limit unless you pass sampling_ratio, which hides most spans of a short test run. The demo passes 1.0 so every span arrives.

Spans: Application Insights maps client and internal spans to the dependencies table and server spans to requests. The customDimensions bag holds the GenAI attributes, so per-agent latency, error rate and token cost are summaries over the same data.

Logs: the distro exports Python logging records to the traces table with the active trace as operation_Id. Keep the line small and safe to retain: agent id and version, operation type, status, token counts, latency and a hashed session id, never the prompt.

One key: the trace_id in the structured log line is the operation_Id you query, so one ID finds the spans and the log line. Reading the tables needs Log Analytics Reader on the connected resources.
