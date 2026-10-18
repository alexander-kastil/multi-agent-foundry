---
slide: "01-01"
topic: 01-tracing
title: "Trace context across services"
subtitle: "The traceparent header carries one trace through the orchestrator, the agent service and Foundry"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/01-01-trace-across-services.svg
order: 3
source: demos/07-evaluation/01-tracing/demo-tracing.md
gamma_prompt: >
  Slide titled "Trace context across services", subtitle "The traceparent header carries one trace
  through the orchestrator, the agent service and Foundry". One half holds five short bullets on W3C
  trace context propagation: a new HTTP request starts a new trace unless the caller sends traceparent,
  the orchestrator injects the header inside its client span, the agent service extracts it as the
  parent of its server span, the Responses API call sends it too so Foundry's own spans join, and one
  operation_Id returns all ten rows. The other half shows the supplied diagram: three stacked lanes
  (orchestrator, downstream agent service, Foundry service) with amber traceparent labels on the hops
  and an Application Insights panel on the right. White background, no extra imagery.
---

# Trace context across services

## Content

- A new HTTP request starts a **new trace** unless the caller sends `traceparent`
- The orchestrator **injects** the header inside its client span
- The agent service **extracts** it as the parent of its server span
- The Responses API call sends `traceparent` too, so **Foundry's spans** join the trace
- One `operation_Id` returns all 10 rows, roles `tracing-demo` and `responsesapi`

## Speaker Notes

This slide follows one customer request of the tracing demo across three services and shows why it stays one trace instead of three.

New trace: an HTTP hop starts a new trace unless the caller sends the W3C traceparent header and the callee reads it. Without the header, each service records its own unconnected trace.

Inject: the orchestrator calls propagator.inject inside its client span orchestrator_to_m7-product-search-agent and sends the headers with the HTTP POST. In C#, HttpClient writes the header from the current Activity, so no inject code is needed.

Extract: the downstream service extracts the context from the request headers and starts invoke_agent m7-product-search-agent as a server span with that parent. That is why the server span nests under the client span of the other service, and why the failed request shows ERROR on both sides of the hop.

Foundry joins: the prompt agent is called through the Responses API, and the SDK client sends traceparent as well. Foundry records its own spans with the role responsesapi, and they land in your trace.

One operation_Id: the query for operation_Id cbec7d2813fb351f08a3042710462eea returns eight rows from tracing-demo and two from responsesapi. The same data appears on the agent's Traces tab in the Foundry portal, usually within two to five minutes.
