# Replay and Resolve a Production Incident

A prompt update goes live, the payment step starts rejecting checkouts, and the traces all show success. You are left with a 50% error rate, a change log and a question: which change, and how do you prove it without touching production again? This demo walks one incident from detection to postmortem and leaves you with a replayable snapshot, a bisection result and a written postmortem.

What you practice here: reproducing a failure from a captured snapshot and isolating the change that caused it.

Duration: about 40 minutes.

## Prerequisites

- The `pro-code-agents` project with a `gpt-5.4` deployment, and `az login` completed.
- Python 3.12 or later and the .NET 10 SDK.

## Run it

Work in `incident-response-py/` for Python or `incident-response-cs/` for C#. Each program publishes two prompt versions of a pricing agent (`m7-pricing-agent` in Python, `m7c3-pricing-agent` in C#), runs 12 checkouts, and deletes both versions when the live part is done. The steps below read the output section by section.

```bash
cd incident-response-py
python -m venv .venv
source .venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
python main.py
```

Set `PROJECT_ENDPOINT` in `.env` first.

```bash
cd incident-response-cs
cp .env.example .env
dotnet run
```

The C# project loads `.env` with DotNetEnv and writes `replay-snapshots/` and `postmortem-output.md` into the folder you run it from.

## Step 1: Produce the incident and classify it

`foundry.py` holds two pricing prompts. `v1` returns the number only; `v2` adds the currency symbol and a sentence of explanation, which is friendlier and breaks `process_payment`, because the payment step accepts only a plain number.

```python
PRICE_PATTERN = re.compile(r"\d+(\.\d{1,2})?")

def process_payment(price_text):
    if PRICE_PATTERN.fullmatch(price_text.strip()):
        return {"status": "paid", "amount": float(price_text.strip())}
    return {"status": "rejected", "reason": "non_numeric_amount"}
```

The script sends six checkouts through order lookup, the pricing agent and payment for each version, records every deployment in a change log and saves a snapshot of every failed trace in `replay-snapshots/`. Severity comes from `classify_severity`.

Expected result:

```text
== Production traffic ==
12 checkouts, 6 failed, error rate 50%, severity P1
```

All six `v1` checkouts are paid and all six `v2` checkouts are rejected.

The C# pipeline is the same three steps. It creates the two versions with `CreateAgentVersionAsync` and calls each one through a Responses client bound to an agent reference.

```csharp
ProjectsAgentVersion agent = await Project.AgentAdministrationClient.CreateAgentVersionAsync(
    agentName: PricingAgentName,
    options: new(new DeclarativeAgentDefinition(Model) { Instructions = prompt }));
```

Expected result (C#):

```text
== Production traffic ==
12 checkouts, 6 failed, error rate 50%, severity P1
```

## Step 2: Find the change with hypotheses

`rca.py` first compares failed and successful requests, then tests hypotheses in order against the change log and stops at the first confirmed one.

```python
def prompt_regression(self):
    change = self.changes_of("prompt_deployment")[-1]
    before = [r for r in self.requests if r["time"] < change["time"]]
    after = [r for r in self.requests if r["time"] >= change["time"]]
    rate_before = sum(not r["success"] for r in before) / max(len(before), 1)
    rate_after = sum(not r["success"] for r in after) / max(len(after), 1)
    return {"hypothesis": "prompt regression", "confirmed": rate_after - rate_before > 0.2}
```

Expected result (timings vary per run):

```text
== Root cause analysis ==
successful: {'count': 6, 'avg_pricing_ms': 1245, 'currency_symbol_rate': 0.0}
failed: {'count': 6, 'avg_pricing_ms': 1488, 'currency_symbol_rate': 1.0}
hypothesis 'model deployment change': eliminated
hypothesis 'prompt regression': CONFIRMED
first failure 2.8s after the prompt deployment
```

Expected result (C#; the dictionaries print without Python's quotes):

```text
== Root cause analysis ==
successful: {count: 6, avg_pricing_ms: 1769, currency_symbol_rate: 0}
failed: {count: 6, avg_pricing_ms: 2035, currency_symbol_rate: 1}
hypothesis 'model deployment change': eliminated
hypothesis 'prompt regression': CONFIRMED
first failure 1.6s after the prompt deployment
```

Latency is the same in both groups; the currency symbol separates them perfectly. That is the pattern a differential is for: it tells you what to ask the change log.

## Step 3: Replay a captured failure

Open one file in `replay-snapshots/`. It holds the input, the model deployment, the prompt with its SHA-256 hash, the tool response with the order id masked as `MOCK-...`, the trace timeline and the production output. `build_snapshot` writes it from the trace recorder.

```python
return {
    "trace_id": recorder.trace_id,
    "input_data": {"order_id": order_id},
    "model_deployments": {"pricing-agent": {"deployment_name": model}},
    "system_prompts": {"pricing-agent": {"content": prompt, "hash": hashlib.sha256(prompt.encode()).hexdigest(), "version": version_label}},
    "tool_responses": {name: {"tool_name": name, "mock_response": {**response, "order_id": "MOCK-" + response["order_id"]}} for name, response in recorder.tool_responses.items()},
    "trace_timeline": recorder.steps,
    "production_output": next(step["output"] for step in recorder.steps if step["name"] == "agent_pricing"),
}
```

The replay feeds the mocked tool response and the captured prompt to the same deployment. The prompts it compares against are read back from the Foundry agent versions with `get_version(...).definition.instructions`.

Expected result (the wording of the explanation varies):

```text
== Replay ==
snapshot 21f42113 production output: '$108.00 — A 10% discount on $120.00 subtracts $12.00, resulting in a final price of $108.00.'
hash check against current v1 prompt: ['pricing-agent: hash mismatch, prompt changed since the production failure']
replay with captured prompt: output '$108.00 — A 10% discount on $120.00 subtracts $12.00, leaving a final price of $108.00.' valid False
bisection {'v1': True, 'v2': False} first failing version v2
```

In C# the snapshot is a typed `record Snapshot` serialized with `System.Text.Json`, and the prompts to compare against come back from `GetAgentVersionAsync` as the `Instructions` of the `DeclarativeAgentDefinition`.

```csharp
ProjectsAgentVersion agent = await Project.AgentAdministrationClient.GetAgentVersionAsync(PricingAgentName, version);
return ((DeclarativeAgentDefinition)agent.Definition).Instructions;
```

Expected result (C#; the wording of the explanation varies):

```text
== Replay ==
snapshot 360e4245 production output: '$108.00 — A 10% discount reduces the $120.00 total by $12.00, resulting in a final price of $108.00.'
hash check against current v1 prompt: [pricing-agent: hash mismatch, prompt changed since the production failure]
replay with captured prompt: output '$108.00 — A 10% discount on $120.00 subtracts $12.00, resulting in a final price of $108.00.' valid False
bisection {v1: True, v2: False} first failing version v2
```

The hash mismatch proves the snapshot came from a different prompt than `v1`. Replaying with the captured prompt reproduces the invalid output without a single production call, and bisection over both versions names `v2` as the first failing one.

## Step 4: Limit the damage with a breaker and a runbook

This part is local and makes no Foundry call. With the primary payment gateway unhealthy, the circuit breaker opens once failures exceed 40% of at least ten calls and serves charges from the backup; after the probe interval, shortened to half a second here, one call goes to the primary and closes the breaker when it succeeds.

```python
if len(self.results) >= 10 and failures / len(self.results) > self.failure_threshold:
    self.state = "open"
    self.opened_at = time.monotonic()
return result if success else fallback()
```

The runbook verifies three failed health checks, switches the configuration, tests a transaction and notifies on call. A second run changes nothing.

Expected result:

```text
== Automated remediation ==
breaker state open, last charges served by ['backup', 'backup', 'backup']
after probe: primary, state closed
runbook run 1: {'status': 'failover_successful', 'action': 'notify_on_call'}
runbook run 2: {'status': 'no_action_needed', 'reason': 'already_on_backup'}
gateway config: {'payment_gateway': 'backup'}
```

The C# breaker and runbook are straight ports; the runbook returns its result as a string in the same shape.

Expected result (C#):

```text
== Automated remediation ==
breaker state open, last charges served by [backup, backup, backup]
after probe: primary, state closed
runbook run 1: {status: failover_successful, action: notify_on_call}
runbook run 2: {status: no_action_needed, reason: already_on_backup}
gateway config: {payment_gateway: backup}
```

The runbook also escalates instead of acting when it is rate limited to three runs per hour, when both gateways are unhealthy and when its five minute deadline passes.

## Step 5: Write the postmortem from the incident data

`postmortem.py` fills a blameless template. The counts and the timing in the summary come from the run; the contributing factors and action items are written in `main.py` because they are decisions, not measurements.

```bash
cat postmortem-output.md
```

Expected result (excerpt):

```text
# Postmortem: Checkout failures after pricing agent prompt update

## Incident summary
After pricing-agent v2 was deployed, 6 of 12 checkouts failed (50%) because the payment step rejected prices containing a currency symbol.

## Root cause
Prompt v2 instructs the pricing agent to include the currency symbol and an explanation, which breaks the numeric contract with the payment step.

## Action items
- Add output format validation to the pricing agent (owner: ai-engineering, due: 2026-10-25)
- Canary every prompt deployment at 5% traffic (owner: platform, due: 2026-11-01)
- Add price format probe to the regression suite (owner: ai-engineering, due: 2026-10-25)
```

Each action item addresses a contributing factor, and none of them is "be more careful".

The C# `Postmortem.Write` fills the same template from a typed `Incident` record with an interpolated raw string literal, and the file it writes has the same sections. Its last console line is `Postmortem written to postmortem-output.md`.

## Verify

- `Production traffic` printed `severity P1` and `replay-snapshots/` holds one JSON file per failed checkout, six for each run.
- The bisection line reports `v2` as the first failing version.
- `postmortem-output.md` exists and its summary says 6 of 12 checkouts failed.

## Cleanup

Each program deletes both versions of its pricing agent itself (`m7-pricing-agent` in Python, `m7c3-pricing-agent` in C#). Remove the generated files if you want a clean folder.

```bash
rm -r replay-snapshots postmortem-output.md
```
