# Trace a Multi-Agent Request Across Services

When an orchestrator hands a customer message to a specialist agent over HTTP, the logs of each service tell half a story and nobody can say which hop was slow or failed. You end this demo holding one trace per request that crosses the orchestrator, the downstream agent service and the Foundry service itself, stored in the Application Insights resource of your project and queryable with KQL.

What you practice here: propagating one trace context across every hop and reading it back as a single tree.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later or the .NET 10 SDK, signed in with `az login`, and the project from the module readme.
- An Application Insights resource connected to the project. `provision.azcli` in this folder creates `m7-appinsights` and connects it; if your project already has one, skip it.
- Log Analytics Reader on the connected Application Insights resource, so you can run the queries in Step 4.

```bash
bash provision.azcli
```

## Run it

Work in `tracing-py/`.

```bash
cd tracing-py
python -m venv .venv
source .venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
```

Set `PROJECT_ENDPOINT` in `.env` to your project endpoint.

The C# twin is `tracing-cs/` with three commands. Its service name is `tracing-demo-cs` and its prompt agents are named `m7c1-`, so its rows are separate from the Python rows in every query below.

```bash
cd tracing-cs
cp .env.example .env
dotnet run -- agent
```

Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in that `.env`.

## Step 1: Trace one Agent Framework agent locally

Agent Framework emits OpenTelemetry spans by default; `configure_otel_providers` installs the provider and the exporters that receive them. Here the exporter is a small `SpanCollector` that keeps the spans in memory so the script can print them as a tree.

```python
collector = SpanCollector()
...
configure_otel_providers(exporters=[collector])
agent = Agent(
    client=client,
    name="order-agent",
    instructions="You answer order questions in one sentence. Use the tool for order status.",
    tools=[get_order_status],
)
with get_tracer().start_as_current_span("customer_request"):
    result = await agent.run("Where is my order 12345?")
```

Run it with `EXPORT_TO_APP_INSIGHTS=false` in `.env`.

```bash
python agent_trace.py
```

Expected result:

```text
Your order 12345 shipped on 2026-10-14 and is expected to arrive on 2026-10-19.

feb5bfbd  customer_request  4057 ms  UNSET
feb5bfbd    invoke_agent order-agent  4057 ms  UNSET
feb5bfbd      chat gpt-5.4  2384 ms  UNSET
feb5bfbd      execute_tool get_order_status  0 ms  UNSET
feb5bfbd      chat gpt-5.4  1671 ms  UNSET
```

The three span names are the GenAI conventions: `invoke_agent` wraps the run, `chat` is each model call and `execute_tool` is the function call between the two model calls. Your own `customer_request` span is the root because the agent spans nest under whatever span is active.

### C#

`AgentTrace.cs` builds the agent with `AsAIAgent` and turns tracing on with `UseOpenTelemetry` on both the chat client and the agent. .NET emits the spans through `System.Diagnostics.ActivitySource`, and `Telemetry.cs` registers the source names with an OpenTelemetry `TracerProvider` and adds the in-memory `SpanCollector` as an exporter.

```csharp
clientFactory: chatClient => chatClient.AsBuilder().UseOpenTelemetry(sourceName: "agent-framework").Build())
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "agent-framework")
    .Build();
```

Run `dotnet run -- agent` with `EXPORT_TO_APP_INSIGHTS=false` in `.env`. Expected result:

```text
Your order 12345 shipped on 2026-10-14 and is arriving on 2026-10-19.

b936f7c8  customer_request  4316 ms  UNSET
b936f7c8    invoke_agent order-agent(11f874c47b42462a93f6aa7953a33f53)  4313 ms  UNSET
b936f7c8      chat gpt-5.4  2418 ms  UNSET
b936f7c8        POST  1402 ms  UNSET
b936f7c8      execute_tool get_order_status  2 ms  UNSET
b936f7c8      chat gpt-5.4  1873 ms  UNSET
b936f7c8        POST  1393 ms  UNSET
```

The span names are the same GenAI conventions. The extra `POST` spans are the HTTP calls of the .NET `HttpClient` under each model call, and .NET appends the agent id to the agent name.

## Step 2: Export the same spans to Application Insights

The project already knows its Application Insights connection string, so `FoundryChatClient.configure_azure_monitor` configures the exporter without a secret in your code. The Azure Monitor distro samples traces at a rate limit by default and drops most spans of a short script, so pass `sampling_ratio=1.0`.

```python
if os.getenv("EXPORT_TO_APP_INSIGHTS", "false").lower() == "true":
    await client.configure_azure_monitor(sampling_ratio=1.0)
    trace.get_tracer_provider().add_span_processor(SimpleSpanProcessor(collector))
else:
    configure_otel_providers(exporters=[collector])
```

Set `EXPORT_TO_APP_INSIGHTS=true` in `.env`, run the script again, wait about two minutes, then count what arrived.

```bash
python agent_trace.py
az monitor app-insights query --app m7-appinsights -g rg-pro-code-agents \
  --analytics-query "dependencies | where timestamp > ago(15m) and cloud_RoleName == 'tracing-demo' | summarize n=count() by name | order by n desc"
```

Expected result (rows of the query):

```text
chat gpt-5.4                   4
customer_request               1
invoke_agent order-agent       1
execute_tool get_order_status  1
```

The local tree and the stored spans are the same spans, so what you debug on your machine is what operations sees later. Each model call appears twice because the Azure SDK instrumentation records its own `chat` span next to the Agent Framework span.

### C#

The exporter is `AddAzureMonitorTraceExporter` from `Azure.Monitor.OpenTelemetry.Exporter`. The connection string comes from the project through `project.Telemetry.GetApplicationInsightsConnectionStringAsync()`, and `AlwaysOnSampler` keeps every span.

```csharp
if (connectionString is not null) builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString);
```

Set `EXPORT_TO_APP_INSIGHTS=true`, run `dotnet run -- agent` again and wait about two minutes. The spans of that run in the `dependencies` table:

```text
chat gpt-5.4                                                2
POST /api/projects/pro-code-agents/openai/v1/responses      2
customer_request                                            1
invoke_agent order-agent(e22e2ad1b3e2408ead9c980257968a64)  1
execute_tool get_order_status                               1
```

## Step 3: Propagate the trace context to a downstream agent

An HTTP hop starts a new trace unless the caller sends the W3C `traceparent` header and the callee reads it. `main.py` runs an orchestrator that routes three customer messages, calls a downstream agent service over HTTP and creates two prompt agents in your project for that service.

```python
from opentelemetry.trace.propagation.tracecontext import TraceContextTextMapPropagator

propagator = TraceContextTextMapPropagator()


def inject_headers():
    headers = {}
    propagator.inject(headers)
    return headers


def extract_context(headers):
    return propagator.extract({key.lower(): value for key, value in headers.items()})
```

The caller injects inside its client span, and the service starts its server span with the extracted context.

```python
with tracer.start_as_current_span(f"orchestrator_to_{target}", kind=SpanKind.CLIENT) as call_span:
    headers = inject_headers()
    reply = httpx.post(f"{base_url}/{target}", json=payload, headers=headers, timeout=120)
```

```python
parent_context = extract_context(self.headers)
with tracer.start_as_current_span(f"invoke_agent {agent_name}", context=parent_context, kind=SpanKind.SERVER) as span:
    span.set_attribute("gen_ai.operation.name", "invoke_agent")
    span.set_attribute("gen_ai.agent.name", agent_name)
```

Run it; this script always exports to Application Insights. The third request goes to an agent that does not exist, so you also get a failed trace.

```bash
python main.py
```

Expected result (the waterfall, trimmed; durations vary):

```text
cbec7d28  customer_request  4294 ms  UNSET
cbec7d28    chat gpt-5.4  2026 ms  UNSET
cbec7d28    orchestrator_to_m7-product-search-agent  2268 ms  UNSET
cbec7d28      POST  2007 ms  UNSET
cbec7d28        invoke_agent m7-product-search-agent  2007 ms  UNSET
cbec7d28          chat  2005 ms  UNSET
...
cc72fa48  customer_request  1385 ms  UNSET
cc72fa48    chat gpt-5.4  1218 ms  UNSET
cc72fa48    orchestrator_to_m7-payment-agent  168 ms  ERROR
cc72fa48      POST  1 ms  ERROR
cc72fa48        invoke_agent m7-payment-agent  1 ms  ERROR

Requests: 3  Distinct trace IDs: 3
Flagged spans: ['POST', 'orchestrator_to_m7-payment-agent', 'invoke_agent m7-payment-agent']
```

The server span nests under the client span of another process, and the error shows on both sides of the hop. The flagged list holds every span with an ERROR status or a duration above 5000 ms.

### C#

.NET needs no inject code: `HttpClient` writes the W3C `traceparent` header from the current `Activity`. The service side still has to read it. `Propagation.cs` parses the header into an `ActivityContext`, and `DownstreamAgent.cs` uses it as the parent of its server span. The service is an `HttpListener` on a free local port.

```csharp
public static ActivityContext Extract(WebHeaderCollection headers) =>
    ActivityContext.TryParse(headers["traceparent"], headers["tracestate"], out ActivityContext context) ? context : default;
```

```csharp
using Activity? span = Tracer.StartActivity($"invoke_agent {agentName}", ActivityKind.Server, parent);
```

Run `dotnet run -- distributed`. It exports spans and logs to Application Insights and creates two `m7c1-` prompt agents. Expected result (waterfall, trimmed; durations vary):

```text
8981ca1e  customer_request  3359 ms  UNSET
8981ca1e    chat gpt-5.4  1436 ms  UNSET
8981ca1e      POST  937 ms  UNSET
8981ca1e    orchestrator_to_m7c1-product-search-agent  1922 ms  UNSET
8981ca1e      POST  1913 ms  UNSET
8981ca1e        invoke_agent m7c1-product-search-agent  1907 ms  UNSET
8981ca1e          chat  1903 ms  UNSET
8981ca1e            POST  1450 ms  UNSET
...
94ca6e6e  customer_request  1504 ms  UNSET
94ca6e6e    chat gpt-5.4  1502 ms  UNSET
94ca6e6e    orchestrator_to_m7c1-payment-agent  2 ms  ERROR
94ca6e6e      POST  2 ms  ERROR
94ca6e6e        invoke_agent m7c1-payment-agent  1 ms  ERROR

Requests: 3  Distinct trace IDs: 3
Flagged spans: ['invoke_agent m7c1-payment-agent', 'POST', 'orchestrator_to_m7c1-payment-agent']
```

The collector counts only traces that start with `customer_request`, so the setup and delete calls of the prompt agents do not add trace ids.

## Step 4: Read the whole trace, Foundry included

The `m7-` prompt agents are called through the Responses API, so Foundry records its own spans for each call. Because the SDK client sends `traceparent` as well, those server-side spans join your trace. Run the fourth query from `queries.kql` with one of your trace IDs; the full trace id is the 32 character value that the structured log lines carry.

```bash
az monitor app-insights query --app m7-appinsights -g rg-pro-code-agents \
  --analytics-query "union dependencies, requests | where operation_Id == '<trace-id>' | project timestamp, itemType, name, role = cloud_RoleName, duration | order by timestamp asc"
```

Expected result for the first request (`operation_Id` `cbec7d2813fb351f08a3042710462eea`, query columns reduced to name, role and duration in ms):

```text
dependency  customer_request                          tracing-demo  4294
dependency  chat gpt-5.4                              tracing-demo  2026
dependency  chat gpt-5.4                              tracing-demo  2025
dependency  orchestrator_to_m7-product-search-agent   tracing-demo  2268
dependency  POST /m7-product-search-agent             tracing-demo  2007
dependency  chat                                      tracing-demo  2005
dependency  invoke_agent m7-product-search-agent      tracing-demo  2005
request     invoke_agent m7-product-search-agent      tracing-demo  2007
dependency  invoke_agent m7-product-search-agent:1    responsesapi  1493
dependency  chat gpt-5.4-2026-03-05                   responsesapi  1364
```

The two rows with role `responsesapi` were produced by the Foundry service, not by your process. Open Build, Agents, one of the agents and its Traces tab in the Foundry portal: Foundry documents that the same data appears there, usually within two to five minutes, for the last 90 days.

### C#

Run the same query as above with an `operation_Id` of the C# run. Expected result for `63070c71176e6ef6df75972d3a7b1e58`:

```text
dependency  customer_request                                         tracing-demo-cs  4104
dependency  chat gpt-5.4                                             tracing-demo-cs  1625
dependency  POST /api/projects/pro-code-agents/openai/v1/responses   tracing-demo-cs  1120
dependency  orchestrator_to_m7c1-product-search-agent                tracing-demo-cs  2478
dependency  POST /m7c1-product-search-agent                          tracing-demo-cs  2468
request     invoke_agent m7c1-product-search-agent                   tracing-demo-cs  2462
dependency  chat                                                     tracing-demo-cs  2458
dependency  POST /api/projects/pro-code-agents/openai/v1/responses   tracing-demo-cs  2002
dependency  invoke_agent m7c1-product-search-agent:1                 responsesapi     1549
dependency  chat gpt-5.4-2026-03-05                                  responsesapi     1324
```

The typed .NET client also sends `traceparent`, so the Foundry spans join the trace in C# too.

## Step 5: Join logs to spans

Each decision is also written as one JSON log line carrying the active `trace_id` and `span_id`, with the session id hashed and no prompt text. The Azure Monitor distro exports Python logs to the `traces` table with the same trace as `operation_Id`, so one key joins logs and spans.

```python
span = trace.get_current_span()
context = span.get_span_context()
entry = {
    "trace_id": format(context.trace_id, "032x"),
    "span_id": format(context.span_id, "016x"),
    "agent_id": self.agent_id,
    "operation_type": operation_type,
    "session_id_hash": hashlib.sha256(session_id.encode()).hexdigest()[:16] if session_id else None,
}
```

The first line of the previous run printed:

```text
{"timestamp": "2026-10-18T07:18:28.772578+00:00", "trace_id": "cbec7d2813fb351f08a3042710462eea", "span_id": "ff6f21150f5720d5", "agent_id": "orchestrator-agent", "agent_version": "1.0.0", "operation_type": "LLM_ROUTING", "status": "success", "session_id_hash": "27ac7a3cd2914d45", "customer_tier": "premium", "input_token_count": 68, "output_token_count": 18, "latency_ms": 0, "model_id": "gpt-5.4", "output": {"target_agent": "product-search-agent", "confidence": 0.99}}
```

The `trace_id` is the `operation_Id` you used in Step 4, so the same ID finds the log line in the `traces` table.

Run the cost query to turn those spans into per-agent token totals.

```kusto
dependencies
| where timestamp > ago(1h) and cloud_RoleName == "responsesapi"
| where name startswith "chat" and tostring(customDimensions["gen_ai.agent.name"]) startswith "m7-"
| extend agent = tostring(customDimensions["gen_ai.agent.name"])
| summarize input_tokens = sum(tolong(customDimensions["gen_ai.usage.input_tokens"])), output_tokens = sum(tolong(customDimensions["gen_ai.usage.output_tokens"])) by agent
```

Expected result for the agents of this run (the query returns a row for every `m7-` agent in the project):

```text
m7-product-search-agent   36   74
m7-order-agent            35   59
```

### C#

`StructuredLog.cs` writes the same JSON line with the `trace_id` and `span_id` of `Activity.Current`. The log exporter is `AddAzureMonitorLogExporter` on an `ILoggerFactory` with `IncludeFormattedMessage = true`, so the JSON arrives as the `message` of the `traces` table.

```csharp
using ILoggerFactory loggers = LoggerFactory.Create(b => b.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.AddAzureMonitorLogExporter(x => x.ConnectionString = connectionString);
}));
```

The first line of the run printed:

```text
{"timestamp":"2026-10-18T07:40:13.001360+00:00","trace_id":"8981ca1ed4377b85c6fd83e52e99d543","span_id":"5f51aaa8f1ad49ea","agent_id":"orchestrator-agent","agent_version":"1.0.0","operation_type":"LLM_ROUTING","status":"success","session_id_hash":"27ac7a3cd2914d45","customer_tier":"premium","input_token_count":68,"output_token_count":18,"latency_ms":0,"output":{"target_agent":"product-search-agent","confidence":0.99}}
```

The `traces` table for `63070c71176e6ef6df75972d3a7b1e58` holds two rows with role `tracing-demo-cs`, one per decision, whose `message` is that JSON. The cost query with `startswith 'm7c1-'` returns:

```text
m7c1-product-search-agent   108   174
m7c1-order-agent            105   171
```

## Step 6: Detect a deviation from the baseline

Spans answer "what happened to this request". A baseline answers "is this normal".

`anomaly.py` builds a 14 day token baseline for an agent, flags values beyond three standard deviations, checks routing drift against last week and folds correlated alerts into one incident through the dependency chain. It makes no Azure calls.

```bash
python anomaly.py
```

Expected result:

```text
tokens=905 z=0.72 anomaly=False severity=warning
tokens=2650 z=88.82 anomaly=True severity=critical
routing share=0.45 z=-11.79 anomaly=True
4 anomalies grouped into 1 incident(s)
```

### C#

`Anomaly.cs` ports the detectors without a library. Run `dotnet run -- anomaly`; the output equals the Python output:

```text
tokens=905 z=0.72 anomaly=False severity=warning
tokens=2650 z=88.82 anomaly=True severity=critical
routing share=0.45 z=-11.79 anomaly=True
4 anomalies grouped into 1 incident(s)
```

## Verify

- Step 3 printed `Distinct trace IDs: 3` for three requests, and the failed request shows `ERROR` on both the client and the server span.
- The `requests` and `dependencies` tables contain rows with `cloud_RoleName` `tracing-demo` and rows with `responsesapi` for the same `operation_Id`.
- The token query returns one row per `m7-` agent.

## Cleanup

`main.py` and `dotnet run -- distributed` delete the two prompt agent versions they create. Keep `m7-appinsights` if you continue with the next demos; otherwise remove it and its connection.

```bash
bash deprovision.azcli
```
