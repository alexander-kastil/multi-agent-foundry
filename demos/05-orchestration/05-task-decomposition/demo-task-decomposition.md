# Decompose a Research Question with a Planner

A single prompt that asks for a stock price, a filing analysis, a macro view and an investment recommendation either gets a vague answer or fails halfway. You cannot fix that by writing a longer prompt. In this demo you split the same investment question three ways: with a validated prompt chain, with a planner agent that returns a dependency graph executed in parallel waves, and with agents that hand work to each other.

What you practice here: turning a request into smaller agent tasks and validating what the splitting step produced.

Duration: about 35 minutes.

## Prerequisites

- Python 3.12, or the .NET 10 SDK for the C# project
- Signed in with `az login` and the **Azure AI User** role on the Foundry project
- A chat model deployment, for example `gpt-5.4`, in the Foundry project
- The handoff builder from [Sequential, Concurrent & Handoff Orchestration](../03-orchestration-patterns/readme.md)

## Run it

```bash
cd demos/05-orchestration/05-task-decomposition/task-decomposition-py
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python decomposition.py chain
```

The C# project in `task-decomposition-cs/` takes the same strategy names, plus `cycle` for Step 5:

```bash
cd demos/05-orchestration/05-task-decomposition/task-decomposition-cs
cp .env.example .env
dotnet run -- chain
dotnet run -- plan
dotnet run -- cycle
dotnet run -- handoff
```

On macOS and Linux use `.venv/bin/python`. Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first. `decomposition.py` accepts `chain`, `plan` and `handoff`, and runs all three when you pass nothing.

## Step 1: Ask agents for JSON and parse it safely

Every strategy exchanges JSON between agents. `ask_json` runs an agent and parses the reply, stripping a Markdown fence when the model adds one. A parse failure raises immediately, which is what you want from a step that feeds code.

```python
def parse_json(text: str) -> dict:
    cleaned = text.strip()
    if cleaned.startswith("```"):
        cleaned = cleaned.split("\n", 1)[1].rsplit("```", 1)[0]
    return json.loads(cleaned)


async def ask_json(agent, prompt: str) -> dict:
    return parse_json((await agent.run(prompt)).text)
```

```csharp
public static JsonNode ParseJson(string text)
{
    string cleaned = text.Trim();
    if (cleaned.StartsWith("```"))
        cleaned = cleaned[(cleaned.IndexOf('
') + 1)..cleaned.LastIndexOf("```")];
    return JsonNode.Parse(cleaned)!;
}

public static async Task<JsonNode> AskJson(AIAgent agent, string prompt) =>
    ParseJson((await agent.RunAsync(prompt)).Text);
```

Each call to `make_agent` creates its own `FoundryChatClient`, and `handoff=True` sets `require_per_service_call_history_persistence` for the handoff strategy.

## Step 2: Run a prompt chain with a validation gate and a branch

The chain extracts metrics from a filing text, checks them in code, classifies the company profile and picks the valuation prompt from that label. The gate raises an `AssertionError` for an impossible margin, so a bad extraction never reaches the valuation agent.

```python
def validate_metrics(metrics: dict) -> dict:
    assert -1.0 <= metrics["operating_margin"] <= 1.0
    assert metrics["debt_to_equity"] >= 0
    return metrics


BRANCHES = {
    "GROWTH_DRIVEN": "Produce a five year discounted cash flow summary ...",
    "MATURE_EARNINGS": "Produce a comparable multiples valuation using P/E and EV/EBITDA versus peers.",
    "ASSET_HEAVY": "Produce a book value based valuation.",
}
```

```csharp
static JsonNode ValidateMetrics(JsonNode metrics)
{
    double margin = metrics["operating_margin"]!.GetValue<double>();
    if (margin < -1.0 || margin > 1.0 || metrics["debt_to_equity"]!.GetValue<double>() < 0)
        throw new InvalidOperationException($"Metrics out of range: {metrics.ToJsonString()}");
    return metrics;
}
```

Run `.venv/Scripts/python decomposition.py chain` or `dotnet run -- chain`.

Expected output (model values vary):

```text
Prompt chain with branching and validation
Step 1 metrics: {"revenue_millions": 250, "revenue_growth_3yr": 4.8139534884, "operating_margin": -0.12, "debt_to_equity": 0.3}
Step 2 profile: GROWTH_DRIVEN
Step 3 valuation method: 5-year DCF
Step 4 commentary: The 5-year DCF suggests limited equity upside: with revenue growing only ~4.8% annually and a -12% operating margin, ...
```

A trimmed C# run prints the same four lines:

```text
Prompt chain with branching and validation
Step 1 metrics: {"revenue_millions":250,"revenue_growth_3yr":4.81,"operating_margin":-0.12,"debt_to_equity":0.3}
Step 2 profile: GROWTH_DRIVEN
Step 3 valuation method: Five-year DCF using revenue growth fade from 4.8% toward 2.5%, margin improvement from negative operating levels toward modest profitability, WACC around 11%, terminal growth 2.5%.
Step 4 commentary: This five-year DCF assumes modest top-line expansion from $250M to roughly $285M, with valuation hinging on a credible turnaround ...
```

The company grew fast, so the classifier chose `GROWTH_DRIVEN`, and the branch table turned that label into a discounted cash flow prompt without another model call.

## Step 3: Let a planner build the dependency graph

The planner receives the query and the registry of specialists and must return raw JSON. The prompt carries the rules: only necessary subtasks, tasks without dependencies can run in parallel, and a complex query ends with the thesis generator.

```python
PLAN_PROMPT = """Research query: "{query}"

Available specialist agents:
{registry}

Return JSON: {{"complexity_assessment": "simple|moderate|complex", "subtasks": [{{"task_id": "T1", "description": "...", "assigned_agent": "registry name", "depends_on": []}}]}}

Rules: include only necessary subtasks, tasks without dependencies can run in parallel, simpler queries need fewer subtasks, a complex query ends with investment_thesis_generator."""
```

The C# project keeps the prompt and the registry as the same text in `Planner.cs` and `Registry.cs`, and fills the prompt with `string.Format`.

The registry lists each specialist with its capabilities and a latency estimate:

```python
"sec_filing_analyzer": {
    "capabilities": ["parse SEC filings", "extract financial metrics", "analyze MD&A sections"],
    "latency_estimate_sec": 15,
},
```

## Step 4: Execute the plan in waves

`execute_plan` repeats one loop. It collects the tasks whose dependencies are all complete, runs them together with `asyncio.gather`, and hands each task the results of the tasks it depends on. Run `.venv/Scripts/python decomposition.py plan` to plan and execute two queries of different size.

```python
ready = [task for task in pending.values() if all(dep in completed for dep in task["depends_on"])]
if not ready:
    raise ValueError(f"Plan has a dependency cycle or an unknown dependency among {list(pending)}")
wave += 1
results = await asyncio.gather(
    *(execute_task(task, {dep: completed[dep] for dep in task["depends_on"]}, query) for task in ready)
)
```

```csharp
List<JsonNode> ready = pending.Values
    .Where(task => task["depends_on"]!.AsArray().All(dep => completed.ContainsKey(dep!.GetValue<string>())))
    .ToList();
if (ready.Count == 0)
    throw new InvalidOperationException($"Plan has a dependency cycle or an unknown dependency among [{string.Join(", ", pending.Keys.Select(id => $"'{id}'"))}]");
string[] results = await Task.WhenAll(ready.Select(task => ExecuteTask(task, ContextFor(task), query)));
```

Run `dotnet run -- plan` for the C# version; `Task.WhenAll` replaces `asyncio.gather` and the output has the same shape, with `depends_on` printed as JSON (`["T1","T2","T3"]`).

Expected output (final texts are model output, shortened here):

```text
Dynamic adaptive decomposition

Query: What is Microsoft's current stock price?
Complexity: simple, subtasks: 1
  T1 -> financial_data_retriever depends_on=[]
Wave 1: ['T1']
Final output: I don’t have live market data access in this chat, so I can’t verify Microsoft’s current price right now. ...

Query: Should Contoso Capital invest in Microsoft given current AI market trends and macroeconomic conditions?
Complexity: complex, subtasks: 4
  T1 -> financial_data_retriever depends_on=[]
  T2 -> sec_filing_analyzer depends_on=[]
  T3 -> economic_indicator_synthesizer depends_on=[]
  T4 -> investment_thesis_generator depends_on=['T1', 'T2', 'T3']
Wave 1: ['T1', 'T2', 'T3']
Wave 2: ['T4']
Final output: **Recommendation: Buy (long-term core holding), with valuation discipline.** ...
```

The simple question produced one task and one wave. The complex one produced the graph from the readme: three independent tasks in wave 1 and the thesis in wave 2 with their results as context. The financial data retriever has no live data source, so its figures are illustrative; the instructions say so.

## Step 5: Reject a plan that cannot run

A planner is a model, so its output is untrusted input. `cycle_check.py` feeds `execute_plan` a plan in which T1 waits for T2 and T2 waits for T1, and without the guard the `while pending` loop would never find a ready task and would spin forever. Run `.venv/Scripts/python cycle_check.py` or `dotnet run -- cycle`.

Expected output:

```text
Plan has a dependency cycle or an unknown dependency among ['T1', 'T2']
```

The same guard catches a `depends_on` that names a task the planner never created. The script does not call a model.

## Step 6: Hand off between specialists

The last strategy has no plan. An SEC filing analyst starts, and its instructions say to hand off to the international risk assessor when foreign revenue exceeds 30 percent. Autonomous mode lets the agents continue without user input, and `turn_limits` caps each agent at two turns.

```python
workflow = (
    HandoffBuilder(
        name="financial_analysis_handoff",
        participants=[sec_analyzer, international_risk, credit_analyst],
    )
    .with_start_agent(sec_analyzer)
    .add_handoff(sec_analyzer, [international_risk])
    .add_handoff(credit_analyst, [international_risk])
    .with_autonomous_mode(turn_limits={"sec_filing_analyzer": 2, "international_risk_assessor": 2})
    .build()
)
```

.NET has no autonomous mode and no turn limits on the handoff builder, so the C# version cannot express the `with_autonomous_mode` line. It builds the same graph with `WithHandoffs`, and the run ends when an agent answers without handing off. The .NET builder also rejects a participant that no handoff reaches, so `creditAnalyst` is made reachable from the SEC analyst.

```csharp
Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(secAnalyzer)
    .WithHandoffs(secAnalyzer, [internationalRisk, creditAnalyst])
    .WithHandoffs(creditAnalyst, [internationalRisk])
    .Build();
```

Run `.venv/Scripts/python decomposition.py handoff` or `dotnet run -- handoff`.

Expected output (JSON content is model output, shortened here):

```text
No handoff configuration found for agent 'international_risk_assessor'. This agent will not be able to hand off to any other agents and your workflow may get stuck.

Handoff collaboration
Agents that spoke in order: ['sec_filing_analyzer', 'international_risk_assessor']
Final message: {
  "currency_risk_score": 8,
  "hedging_recommendations": [
    "Prioritize hedge coverage for Turkish lira exposure, then Brazilian real, using rolling forwards or NDFs where deliverability is limited.",
    ...
```

In the C# run the SEC analyst handed off without writing any text, so only the assessor appears in the list:

```text
Handoff collaboration
Agents that spoke in order: ['international_risk_assessor']
Final message: {
  "currency_risk_score": 8,
  "hedging_recommendations": [
    "Increase use of rolling FX forwards and options to hedge forecasted BRL, INR, and TRY cash flows, ...
```

Foreign revenue in the prompt is 60 percent, so the analyst handed off. The first line is the builder warning about the terminal specialist: the risk assessor has no outgoing handoff by design, so the run ends there.

## Verify

- The chain output shows a numeric `operating_margin` and a profile label before the valuation method.
- The complex query prints `Wave 1` with three tasks and `Wave 2` with `T4`.
- `cycle_check.py` prints the dependency cycle message and exits without hanging.
- The Python handoff run lists both agents in `Agents that spoke in order`; the C# run lists the assessor, which answered after the hand-off.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `json.decoder.JSONDecodeError` in `ask_json` | The model added prose around the JSON | Repeat "Reply with raw JSON only" in the instructions, or tighten the parser |
| `AssertionError` in `validate_metrics` | The extractor returned an out-of-range value | This is the gate working; inspect the extractor reply and fix the prompt |
| `KeyError` for `assigned_agent` | The planner invented an agent that is not in the registry | Validate `assigned_agent` against `AGENT_REGISTRY` before executing |
| `ValueError: Handoff workflows require all participant agents to have 'require_per_service_call_history_persistence=True'` | A handoff participant was created without the flag | Call `make_agent(..., handoff=True)` |

## Cleanup

Nothing is created in Azure. All agents are in-memory objects.

## Next

Make orchestration survive failing agents with [Resilient Orchestration: Quorum & Supervisors](../06-resilient-orchestration/readme.md).
