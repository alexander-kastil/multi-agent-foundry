# Make an MCP Server Survive a Backend Outage

When the system behind a tool fails, an agent should say so and follow a safe fallback, not guess. You will run an MCP server with retry, circuit breaker and routing, force an outage while you watch the breaker open and close, and then put a Foundry-hosted agent on top to see what it does with the fallback. A C# server with Polly repeats the pattern.

## Prerequisites

- Python 3.12 and .NET 10 SDK.
- `az login` and a `gpt-5.4` deployment in your Foundry project for step 4. Steps 1 to 3 and 5 need no Azure resources.
- Ports 8765 (Python) and 47030 (C#) free.

## Run it

```powershell
cd demos/02-mcp/05-mcp-resilience/mcp-resilience-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
copy .env.example .env
```

Set `MCP_API_KEY` in `.env` to any string, and set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` for step 4. `probe.py` and `agent.py` start `server.py` themselves and stop it when they finish.

## Step 1: Read the server

`server.py` registers three backend instances and one tool. The tool never raises to the model. It maps every failure to a result with a `status`.

```python
east = DrugInteractionInstance("drugs-east", "1.0.0", 0.15)
west = DrugInteractionInstance("drugs-west", "1.0.0", 0.05)
canary = DrugInteractionInstance("drugs-canary", "1.1.0", 0.02)

registry.register_instance("lookup_drug_interaction", "drugs-canary", "1.1.0", canary, health_status="unhealthy")
circuit = CircuitBreaker(CircuitBreakerConfig(failure_threshold=3, recovery_timeout=10.0, success_threshold=2))


@mcp_retry_with_backoff(max_attempts=3, base_delay_seconds=0.2, max_delay_seconds=2)
def call_backend(drug_a, drug_b):
    return router.resolve_and_call("lookup_drug_interaction", drug_a=drug_a, drug_b=drug_b)
```

`call_backend` retries transient errors. The circuit wraps `call_backend`, so one failed tool call, after its retries, counts once toward the threshold of three. A middleware rejects any request without the `x-api-key` header, with a constant-time comparison.

```python
class ApiKeyMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request, call_next):
        if not hmac.compare_digest(request.headers.get("x-api-key", "").encode(), API_KEY.encode()):
            return JSONResponse({"error": "unauthorized"}, status_code=401)
        return await call_next(request)
```

![The MCP tool wraps the circuit breaker, which wraps the retry, which wraps the router over drugs-east, drugs-west and drugs-canary; on the right the four results the model receives: ok, invalid_input, retries_exhausted and service_circuit_open](../pptx/images/05-01-resilience-layers.svg)

## Step 2: Watch routing and the permanent error

`probe.py` is an MCP client with no model. It calls the tool directly and prints the server's health and metrics after each phase. Run it.

```powershell
python probe.py
```

The first two sections show the guard rail and the error class that must not be retried.

```text
=== Authentication ===
no key   -> 401
with key -> 200

=== Permanent error returns immediately ===
{'status': 'error', 'error_type': 'invalid_input', 'message': 'invalid medication code: zorbiflex'}
took 0.16s, no retry
```

The unknown medication code is a `PermanentError`, so the call returns after one backend delay. The next two sections show where the traffic went.

```text
=== Latency-based routing ===
after 8 calls: circuit=closed routing={'drugs-east (latency_based_warmup)': 1, 'drugs-west (latency_based_warmup)': 1, 'drugs-west (latency_based)': 7} p95_ms={'drugs-east': 150.3, 'drugs-west': 50.5, 'drugs-canary': 0.0}

=== Weighted canary at 30 percent ===
after 30 calls: circuit=closed routing={'drugs-east (latency_based_warmup)': 1, 'drugs-west (latency_based_warmup)': 1, 'drugs-west (latency_based)': 7, 'drugs-west (weighted)': 19, 'drugs-canary (weighted)': 11} p95_ms={'drugs-east': 150.3, 'drugs-west': 50.6, 'drugs-canary': 20.6}
```

The router measured both instances once, then sent the traffic to the faster `drugs-west`. The canary receives about 30 percent of the next 30 calls, which is 11 here, and the exact split varies from run to run.

## Step 3: Trigger the outage and the breaker

The next section turns on an outage and makes four calls. This is the heart of the demo.

```text
=== Outage: retries, circuit breaker, fallback ===
first failing call took 0.9s (retries with backoff)
after 3 failed calls: circuit=open routing={...}
open circuit answered in 0.00s -> {'status': 'unavailable', 'reason': 'service_circuit_open', 'fallback': 'consult_pharmacist', 'retry_after_seconds': 9.8, 'message': 'Circuit open, next attempt in 9.8s'}
```

A failing call takes about 0.9 seconds because it makes three attempts with growing delays. After three of them the circuit is `open`, and the next call is answered in milliseconds with the fallback and a `retry_after_seconds` hint. The recovery section turns the outage off, waits for the recovery timeout and makes two calls.

```text
=== Recovery ===
after recovery: circuit=closed routing={...}
```

The breaker moved from open to half-open to closed without a restart, because the first call after ten seconds was a trial, and the second success closed it.

![Timeline of the probe: calls 1 to 3 make three attempts each and return retries_exhausted, the circuit opens after the third, call 4 returns service_circuit_open in 0.00 s, and after the 10 s recovery timeout calls 5 and 6 succeed while the circuit goes half_open and then closed](../pptx/images/05-02-outage-timeline.svg)

## Step 4: Put an agent on top

`agent.py` starts the same server and connects a Foundry-hosted model through Agent Framework. The instructions carry the contract for the fallback.

```python
INSTRUCTIONS = (
    "You are a clinical decision support assistant for Northwind Health. "
    "Use the lookup_drug_interaction tool for every interaction question. "
    "If the tool reports status unavailable, say the check could not be completed, "
    "never state that no interaction exists, and follow the fallback it names. "
    "Keep answers under 60 words."
)

mcp_tool = MCPStreamableHTTPTool(name="drug-tools", url=f"{BASE}/mcp", static_headers=HEADERS, approval_mode="never_require")
```

```powershell
python agent.py
```

Expected result (the wording of the answers varies, the behavior does not):

```text
=== Healthy server ===
Warfarin plus aspirin has a major interaction. They can additively increase bleeding risk. ...

=== Permanent error ===
I couldn’t complete the interaction check: “zorbiflex” isn’t recognized as a valid medication name/code. Please verify the exact drug name ...

=== Outage: the agent follows the fallback ===
The interaction check could not be completed because the database was unavailable. Please consult a pharmacist to assess whether atorvastatin and clarithromycin are safe to combine.
first failing question took 4.3s
after the outage question: circuit=closed routing={...}

=== Recovery ===
Use caution ... The interaction is major: clarithromycin inhibits CYP3A4 ...
after recovery: circuit=closed routing={...}
```

During the outage the agent reports the failure and names the pharmacist, and it does not claim that the combination is safe. One failed question counts once toward the breaker, so the circuit is still `closed` after it. The 4.3 seconds include the retries and the model calls.

## Step 5: Do it in C# with Polly

`mcp-resilience-cs` builds the same server on the C# SDK. A `ResiliencePipeline` from `Polly.Core` composes the breaker and the retry, and the tool returns the same fallback shape.

```csharp
public async Task<object> LookupAsync(DrugBackend backend, string drugA, string drugB, CancellationToken cancellationToken)
{
    try
    {
        return await pipeline.ExecuteAsync(async token => await backend.LookupAsync(drugA, drugB, token), cancellationToken);
    }
    catch (BrokenCircuitException)
    {
        return new { status = "unavailable", reason = "service_circuit_open", fallback = "consult_pharmacist" };
    }
    catch (TransientBackendException)
    {
        return new { status = "unavailable", reason = "retries_exhausted", fallback = "consult_pharmacist" };
    }
}
```

Start the server in one terminal and run the probe in another. `MCP_API_KEY` must be the same in both.

```powershell
cd ../mcp-resilience-cs
dotnet build
$env:MCP_API_KEY = "change-me"
dotnet run --no-build
```

```powershell
$env:MCP_API_KEY = "change-me"
dotnet run --no-build -- --probe
```

Expected result:

```text
no key   -> 401
with key -> 200

=== Healthy ===
{"status":"ok","drugA":"warfarin","drugB":"aspirin","severity":"major","note":"Additive bleeding risk"}

=== Outage: retries, then the circuit opens ===
call 1: 776 ms {"status":"unavailable","reason":"retries_exhausted","fallback":"consult_pharmacist"}
call 2: 755 ms {"status":"unavailable","reason":"retries_exhausted","fallback":"consult_pharmacist"}
call 3: 1 ms {"status":"unavailable","reason":"service_circuit_open","fallback":"consult_pharmacist"}
call 4: 0 ms {"status":"unavailable","reason":"service_circuit_open","fallback":"consult_pharmacist"}
{"circuit":"Open"}

=== Recovery ===
{"status":"ok","drugA":"atorvastatin","drugB":"clarithromycin","severity":"major","note":"CYP3A4 inhibition raises statin exposure"}
{"circuit":"Closed"}
```

Polly counts a failure ratio over a sampling window and not consecutive failures. The healthy call in the window means the circuit opens at the second failure here, with `FailureRatio = 0.5` and `MinimumThroughput = 3`. With `FailureRatio = 1.0` the same sequence never opened the circuit, because four failures plus one success is a ratio of 0.8.

## Verify

You are done when all of these hold:

- The unknown medication returns in about 0.2 seconds and does not retry.
- After the outage starts, the circuit reports `open` and the next call returns in milliseconds with `service_circuit_open`.
- After ten seconds without an outage, calls succeed and the circuit reports `closed`.
- The agent never says that the interaction does not exist while the backend is down.

## Cleanup

`probe.py` and `agent.py` stop the Python server on exit. Stop the C# server with Ctrl+C. Nothing was created in Azure.

```powershell
Get-NetTCPConnection -LocalPort 8765,47030 -State Listen -ErrorAction SilentlyContinue
```

The command prints nothing once both ports are free.
