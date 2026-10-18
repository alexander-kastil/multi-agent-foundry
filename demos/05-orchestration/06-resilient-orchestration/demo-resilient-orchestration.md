# Keep a Research Hub Running When Spokes Fail

A research hub that calls four specialist agents is only as reliable as its slowest, flakiest specialist. One timeout and the whole report fails, or worse, the report is written as if the missing section did not matter. In this demo you wrap every specialist call in an envelope, add timeouts, a circuit breaker and a concurrency limit, decide how many parallel answers are enough, and add a supervisor that refuses a weak report.

What you practice here: turning agent failures into data the orchestrator can reason about.

Duration: about 35 minutes.

## Prerequisites

- Python 3.12, or the .NET 10 SDK for the C# project
- Signed in with `az login` and the **Azure AI User** role on the Foundry project
- A chat model deployment, for example `gpt-5.4`, in the Foundry project
- The planner and wave concepts from [Task Decomposition: Planner & Waves](../05-task-decomposition/readme.md)

## Run it

```bash
cd demos/05-orchestration/06-resilient-orchestration/resilient-orchestration-py
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python orchestration.py hub
```

The C# project in `resilient-orchestration-cs/` takes the same stage names:

```bash
cd demos/05-orchestration/06-resilient-orchestration/resilient-orchestration-cs
cp .env.example .env
dotnet run -- hub
dotnet run -- quorum
dotnet run -- circuit hub
dotnet run -- supervisor
```

On macOS and Linux use `.venv/bin/python`. Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first. `orchestration.py` takes the stage names `hub`, `quorum`, `circuit` and `supervisor`, in any order, and runs all four when you pass none.

## Step 1: Wrap every spoke call in an envelope

Four specialist agents exist as plain agents: market analysis, risk assessment, compliance check and ESG evaluation. `invoke_spoke` is the single door to them. It applies a timeout, takes a slot from a semaphore that allows two concurrent spoke calls, consults the circuit breaker and always returns a dictionary, never an exception.

![The hub calls every spoke through invoke_spoke: a circuit check, a semaphore for two calls and a timeout each end in a status, and every status returns to the hub in one JSON envelope](../pptx/images/06-01-spoke-envelope.svg)

```python
async def invoke_spoke(spoke_name: str, request: str, timeout: float = SPOKE_TIMEOUT) -> dict:
    if circuit_open(spoke_name):
        return {"spoke": spoke_name, "status": "circuit_open", "result": None}
    async with SEMAPHORE:
        try:
            result = await asyncio.wait_for(SPOKES[spoke_name].run(request), timeout=timeout)
        except asyncio.TimeoutError:
            record_outcome(spoke_name, False)
            return {"spoke": spoke_name, "status": "timeout", "result": None}
        except Exception as ex:
            record_outcome(spoke_name, False)
            return {"spoke": spoke_name, "status": "error", "result": str(ex)}
    record_outcome(spoke_name, True)
    return {"spoke": spoke_name, "status": "success", "result": result.text}
```

```csharp
public static async Task<SpokeOutcome> InvokeSpoke(string spokeName, string request, TimeSpan? timeout = null)
{
    if (CircuitOpen(spokeName))
        return new SpokeOutcome(spokeName, "circuit_open", null);
    await Semaphore.WaitAsync();
    try
    {
        using CancellationTokenSource cts = new(timeout ?? SpokeTimeout);
        AgentResponse result = await SpokeAgents[spokeName].RunAsync(request, cancellationToken: cts.Token);
        RecordOutcome(spokeName, true);
        return new SpokeOutcome(spokeName, "success", result.Text);
    }
    catch (OperationCanceledException)
    {
        RecordOutcome(spokeName, false);
        return new SpokeOutcome(spokeName, "timeout", null);
    }
    catch (Exception ex)
    {
        RecordOutcome(spokeName, false);
        return new SpokeOutcome(spokeName, "error", ex.Message);
    }
    finally
    {
        Semaphore.Release();
    }
}
```

The C# timeout is a `CancellationTokenSource` handed to `RunAsync`, and `SemaphoreSlim` plays the part of the Python semaphore.

## Step 2: Run the hub with the spokes as tools

The hub agent has one tool per spoke, each returning the JSON envelope. Its instructions fix the order and tell it what to do with a spoke that did not succeed.

```python
HUB_INSTRUCTIONS = (
    "You coordinate investment research for Contoso Capital by delegating to specialist agents. "
    "Always complete market analysis before you invoke risk assessment, and run compliance last. "
    "Each tool result carries a status; when a spoke did not succeed, say what is missing and continue with what you have. "
    "Finish with a short research report."
)
```

In C# each spoke tool is an async method that `AIFunctionFactory.Create` turns into a tool:

```csharp
[Description("Assesses investment risk based on a market analysis result.")]
static async Task<string> InvokeRiskAssessmentAgent([Description("Market analysis text the risk assessment is based on")] string marketData) =>
    JsonSerializer.Serialize(await Resilience.InvokeSpoke("risk_assessment", $"Assess risk based on: {marketData}"));
```

Run `.venv/Scripts/python orchestration.py hub` or `dotnet run -- hub`.

Expected output (report wording varies):

```text
hub
Research report: Equities exposure for Contoso growth portfolio

Market analysis
- Equities are currently range-bound.
- Supportive factors include resilient earnings and selective AI-led growth.
...

Risk assessment
- Risk score: 6/10
...

Compliance check
- Status: PASS
- A diversified increase in equities is consistent with a moderate risk profile and avoids excessive concentration in mega-cap technology.

Short research report
A measured increase in equities is supportable for the Contoso growth portfolio. ...
```

In the C# run the compliance spoke answered `FAIL` because the request carried no portfolio details, and the hub reported exactly what was missing instead of inventing a pass:

```text
hub
Research report: Adding equities exposure to the Contoso growth portfolio

Market analysis
- Status: success
...
Compliance review
- Status: success
- Outcome: FAIL
- Missing information:
  - Current portfolio concentration
  - Proposed position size
  - Client objectives and constraints
```

With every spoke healthy the hub calls market analysis, then risk, then compliance, and the report cites all three.

## Step 3: Decide how many parallel answers are enough

`parallel_research` calls market analysis, risk assessment and ESG evaluation at once, each with its own timeout, and counts the successes. The caller passes `required=2`, so two of three answers form a quorum.

```python
results = await asyncio.gather(
    *(invoke_spoke(name, query, timeouts[name]) for name in SPOKE_NAMES),
    return_exceptions=True,
)
successes = {r["spoke"]: r["result"] for r in results if isinstance(r, dict) and r["status"] == "success"}
if len(successes) < required:
    return {"status": "insufficient_quorum", "successful": len(successes), "required": required, "elapsed_sec": elapsed}
```

```csharp
SpokeOutcome[] results = await Task.WhenAll(SpokeNames.Select(name => Resilience.InvokeSpoke(name, query, timeouts[name])));
List<string> successes = results.Where(r => r.Status == "success").Select(r => r.Spoke).Order().ToList();
```

Run `.venv/Scripts/python orchestration.py quorum` or `dotnet run -- quorum`. The script runs three cases: all healthy, ESG timing out after 0.01 seconds, and both market analysis and ESG timing out.

Expected output:

```text
quorum
All spokes healthy
{"status": "success", "completed": ["esg_evaluation", "market_analysis", "risk_assessment"], "missing": [], "elapsed_sec": 6.3}
ESG times out, two of three is enough
{"status": "success", "completed": ["market_analysis", "risk_assessment"], "missing": ["esg_evaluation"], "elapsed_sec": 3.5}
Market analysis and ESG time out
{"status": "insufficient_quorum", "successful": 1, "required": 2, "elapsed_sec": 2.1}
```

The C# run prints the same three shapes as compact JSON. The first case took longer there (28.3 seconds in the run below) because the semaphore lets only two spokes run at once.

```text
{"status":"success","completed":["esg_evaluation","market_analysis","risk_assessment"],"missing":[],"elapsed_sec":28.3}
{"status":"success","completed":["market_analysis","risk_assessment"],"missing":["esg_evaluation"],"elapsed_sec":3.5}
{"status":"insufficient_quorum","successful":1,"required":2,"elapsed_sec":2.9}
```

One missing optional spoke still returns a usable result and names what is missing. Two missing spokes drop below the quorum, and the function refuses to answer from a single opinion.

![Three quorum cases with required=2: three successes and two successes give success, one success gives insufficient_quorum](../pptx/images/06-03-quorum-parallel-spokes.svg)

## Step 4: Trip the circuit breaker

Repeated failures should stop traffic to a spoke. The `circuit` stage calls the compliance spoke four times with a 10 millisecond timeout so that every call times out. After the third failure the circuit opens.

```python
async def trip_circuit(spoke_name: str) -> None:
    for attempt in range(1, 5):
        outcome = await invoke_spoke(spoke_name, "Review MSFT for the growth portfolio.", timeout=0.01)
        print(f"Call {attempt}: {outcome['status']}, failures={CIRCUITS[spoke_name]['failures']}")
```

Run `.venv/Scripts/python orchestration.py circuit` or `dotnet run -- circuit`; the C# output is identical.

Expected output:

```text
circuit
Call 1: timeout, failures=1
Call 2: timeout, failures=2
Call 3: timeout, failures=3
Call 4: circuit_open, failures=3
```

The fourth call never reached the model: the breaker answered `circuit_open` immediately, and the failure count stays at 3. It stays open for 60 seconds, then the next call is allowed through.

![Four calls to compliance_check: three pass a closed breaker and time out with failures 1 to 3, and the fourth gets circuit_open at once without reaching the model](../pptx/images/06-02-circuit-breaker.svg)

## Step 5: Watch the hub degrade gracefully

Run the `circuit` and `hub` stages in one process so the open circuit is still in memory when the hub starts: `.venv/Scripts/python orchestration.py circuit hub` or `dotnet run -- circuit hub`. The hub calls compliance last and receives `circuit_open`.

Expected output after the circuit lines (report wording varies):

```text
hub
Research report: Equities exposure for the Contoso growth portfolio

Market analysis
- Status: success
...

Risk assessment
- Status: success
- Risk score: 6/10
...

Compliance review
- Status: circuit_open
- Missing: compliance confirmation on concentration and suitability rules for the proposed allocation.
- Because compliance did not succeed, the proposal should not be finalized until those checks are completed.

Short research report
Adding equities exposure to the growth portfolio appears reasonable only on a selective basis, ...
```

The report says what is missing and does not pretend compliance passed. This is the payoff of returning statuses instead of raising: the model can reason about the gap.

## Step 6: Gate the report with a supervisor

A checker agent returns a JSON verdict and a maker agent rewrites the report using the listed issues. The loop allows one retry. The input is a deliberately weak report: `We looked at the market. Equities seem fine.`

```python
verdict = json.loads((await checker.run(report)).text)
print(f"Supervisor attempt {attempt + 1}: {verdict['verdict']} {verdict['issues']}")
if verdict["verdict"] == "APPROVE":
    return {"report": report, "approved": True, "attempts": attempt + 1}
if attempt < max_retries:
    report = (await maker.run(f"Report:\n{report}\n\nIssues:\n{json.dumps(verdict['issues'])}")).text
```

```csharp
JsonNode verdict = JsonNode.Parse((await checker.RunAsync(report)).Text)!;
string label = verdict["verdict"]!.GetValue<string>();
if (label == "APPROVE")
    return (true, attempt + 1);
if (attempt < maxRetries)
    report = (await maker.RunAsync($"Report:
{report}

Issues:
{verdict["issues"]!.ToJsonString()}")).Text;
```

Run `.venv/Scripts/python orchestration.py supervisor` or `dotnet run -- supervisor`.

Expected output (issue wording varies, and the verdicts are model judgements, not guaranteed):

```text
supervisor
Supervisor attempt 1: REVISE ['Missing recommendation: the report does not state a clear action or conclusion (e.g., buy/hold/sell, increase/decrease exposure, or maintain current positioning).', 'Missing risk statement: ...
Supervisor attempt 2: APPROVE []
Approved: True after 2 attempt(s)
```

The checker rejected the weak report with specific issues, the maker fixed them, and the second attempt passed. If the maker had not fixed them, the result would be `Approved: False after 2 attempt(s)` and the caller would decide what to do.

## Verify

- `quorum` shows `success` with `missing: []`, `success` with one missing spoke, and `insufficient_quorum`.
- `circuit` shows `circuit_open` on call 4 with the failure count unchanged.
- The `circuit hub` run reports `Status: circuit_open` for compliance and states what is missing.
- `supervisor` prints a `REVISE` attempt before the final `Approved` line.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `json.decoder.JSONDecodeError` in the supervisor | The checker wrapped its JSON in a code fence | Keep "without code fences" in the checker instructions or parse as in the decomposition project |
| `insufficient_quorum` when you expected `success` | A per-spoke timeout in the `timeouts` dictionary is too small; 0.01 seconds is deliberately unreachable | Use realistic values such as `90` for the spokes that must answer |
| `circuit_open` persists after a fix | The cooldown has not elapsed or the breaker state is held by a long-running process | Wait `COOLDOWN_SECONDS` or restart the process |
| `DefaultAzureCredential failed to retrieve a token` | No signed-in Azure session | Run `az login` and retry |

## Cleanup

Nothing is created in Azure. Circuit state exists only inside the running Python or .NET process.

## Next

Move from code-defined orchestration to YAML and functional workflows, and plan the migration of Foundry portal workflows, in [Declarative YAML & Functional Workflows](../07-declarative-workflows/readme.md).
