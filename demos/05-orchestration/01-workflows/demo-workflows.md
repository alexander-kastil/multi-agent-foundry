# Route Invoices with a Workflow Graph

Invoice handling usually starts as one long script: parse, check, decide, notify. Adding a second approval path or a parallel fraud check means rewriting the middle of that script and hoping nothing else breaks. In this demo you build the same process as a graph of small executors, watch it run in supersteps, put a Foundry agent in one node and render the result as a diagram.

What you practice here: expressing a process as executors and edges instead of control flow.

Duration: about 30 minutes.

## Prerequisites

- Python 3.12 and the .NET 10 SDK (for the C# step)
- Signed in with `az login` and the **Azure AI User** role on the Foundry project
- A chat model deployment, for example `gpt-5.4`, in the Foundry project

## Run it

```bash
cd demos/05-orchestration/01-workflows/workflows-py
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python pipeline.py
```

On macOS and Linux use `.venv/bin/python`. Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env`; only Step 4 calls a model.

## Step 1: Build a linear pipeline from executors

Start with the two ways to define a node. `ParseInvoice` is a class with an `@handler`, `add_vat` and `format_receipt` are plain async functions turned into executors with `@executor`. The type argument of `WorkflowContext` is the contract: `WorkflowContext[Invoice]` sends `Invoice` messages, `WorkflowContext[Never, str]` yields `str` outputs.

```python
class ParseInvoice(Executor):
    @handler
    async def parse(self, line: str, ctx: WorkflowContext[Invoice]) -> None:
        number, vendor, description, amount = line.split(",")
        await ctx.send_message(Invoice(number, vendor, description, float(amount)))


@executor(id="format_receipt")
async def format_receipt(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number} | {invoice.vendor} | {invoice.amount:.2f} EUR incl. VAT")
```

The builder names the start node, designates the output node with `output_from` and connects the nodes with direct edges. `build()` validates that each edge connects matching types, so a mismatched handler fails here and not in the middle of a run.

```python
workflow = (
    WorkflowBuilder(start_executor=parse, output_from=[format_receipt])
    .add_edge(parse, add_vat)
    .add_edge(add_vat, format_receipt)
    .build()
)
result = await workflow.run(line)
print(result.get_outputs()[0])
```

Run `.venv/Scripts/python pipeline.py`.

Expected output:

```text
INV-001 | Contoso Ltd | 504.00 EUR incl. VAT
INV-002 | Fabrikam Inc | 3300.60 EUR incl. VAT
```

## Step 2: Route with conditional and switch-case edges

Real invoices take different paths. A conditional edge forwards a message only when its predicate is true, so two edges out of `parse_invoice` split valid amounts from invalid ones. A switch-case group then picks exactly one approval path, and `Default` catches everything the cases do not.

```python
workflow = (
    WorkflowBuilder(
        start_executor=parse_invoice,
        output_from=[reject, auto_approve, manager_review, director_review],
    )
    .add_edge(parse_invoice, classify, condition=lambda invoice: invoice.amount > 0)
    .add_edge(parse_invoice, reject, condition=lambda invoice: invoice.amount <= 0)
    .add_switch_case_edge_group(
        classify,
        [
            Case(condition=lambda invoice: invoice.amount < 1000, target=auto_approve),
            Case(condition=lambda invoice: invoice.amount < 10000, target=manager_review),
            Default(target=director_review),
        ],
    )
    .build()
)
```

Run `.venv/Scripts/python routing.py`. The script feeds all four sample invoices through the same workflow object.

Expected output:

```text
INV-001: auto-approved (420.00)
INV-002: sent to the manager (2750.50)
INV-003: sent to the director (18400.00)
INV-004: rejected, amount -1.0 is not valid
```

## Step 3: Fan out, fan in and watch the supersteps

Duplicate, vendor and budget checks are independent, so they should run side by side. `add_fan_out_edges` sends the parsed invoice to all three, and `add_fan_in_edges` collects one `Check` from each into a single list for `aggregate`. Each check sleeps for one second so the timing proves the parallelism.

```python
workflow = (
    WorkflowBuilder(start_executor=parse_invoice, output_from=[aggregate])
    .add_fan_out_edges(parse_invoice, [duplicate_check, vendor_check, budget_check])
    .add_fan_in_edges([duplicate_check, vendor_check, budget_check], aggregate)
    .build()
)
```

Streaming the run exposes the lifecycle events, including the superstep boundaries.

```python
async for event in workflow.run(RAW_INVOICES[2], stream=True):
    if event.type == "superstep_started":
        print(f"superstep {event.iteration} started at {time.perf_counter() - started:.1f}s")
    elif event.type == "executor_invoked":
        print(f"  invoked {event.executor_id}")
```

Run `.venv/Scripts/python fanout.py`.

Expected output:

```text
superstep 1 started at 0.0s
  invoked parse_invoice
superstep 2 started at 0.0s
  invoked duplicate_check
  invoked vendor_check
  invoked budget_check
superstep 3 started at 1.0s
  invoked aggregate
output: failed: budget
total 1.0s for three one-second checks
```

Three one-second checks finish in one second because they share superstep 2. `aggregate` only starts in superstep 3, after the barrier, and it is called once with all three results.

## Step 4: Put a Foundry agent inside the graph

An agent is a node like any other. Pass it to `add_edge` and the framework wraps it in an agent executor. The upstream executor sends an `AgentExecutorRequest` so the agent sees a real user message, and the downstream executor reads the reply from `AgentExecutorResponse`.

```python
@executor(id="parse_invoice")
async def parse_invoice(line: str, ctx: WorkflowContext[AgentExecutorRequest]) -> None:
    number, vendor, description, amount = line.split(",")
    invoice = Invoice(number, vendor, description, float(amount))
    text = f"Invoice {invoice.number} from {invoice.vendor} for {invoice.description}: {invoice.amount} EUR"
    await ctx.send_message(AgentExecutorRequest(messages=[Message("user", [text])], should_respond=True))


@executor(id="publish_decision")
async def publish_decision(response: AgentExecutorResponse, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(response.agent_response.text)
```

```python
reviewer = client.as_agent(name="invoice_reviewer", instructions=REVIEWER_INSTRUCTIONS)
workflow = (
    WorkflowBuilder(start_executor=parse_invoice, output_from=[publish_decision])
    .add_edge(parse_invoice, reviewer)
    .add_edge(reviewer, publish_decision)
    .build()
)
```

Run `.venv/Scripts/python agent_step.py`.

Expected output (the wording of the reason is model output and varies):

```text
APPROVE - Amount is 420.0 EUR, which is below the 10000 threshold.
ESCALATE - Amount is 18400.0 EUR, which is above the 10000 threshold.
```

## Step 5: Render the graph

The routing workflow from Step 2 is easy to read as code, but a diagram is what you put into a review. `WorkflowViz` converts the built workflow to Mermaid and writes it next to the run output.

```python
viz = WorkflowViz(build_workflow())
mermaid = viz.to_mermaid()
Path("output").mkdir(exist_ok=True)
Path("output/invoice-routing.mmd").write_text(mermaid, encoding="utf-8")
```

Run `.venv/Scripts/python viz.py`.

Expected output:

```text
flowchart TD
  parse_invoice["parse_invoice (Start)"];
  classify["classify"];
  reject["reject"];
  auto_approve["auto_approve"];
  manager_review["manager_review"];
  director_review["director_review"];
  parse_invoice -. conditional .-> classify;
  parse_invoice -. conditional .-> reject;
  classify --> auto_approve;
  classify --> manager_review;
  classify --> director_review;
```

Paste the text into any Mermaid renderer. Conditional edges are dotted and labelled, the switch-case edges out of `classify` are solid.

## Step 6: Rebuild routing and fan-out in C#

The C# project mirrors Steps 2 and 3 without a model call, so it needs no Azure access. Executors are `partial` classes with `[MessageHandler]` methods, and the `Microsoft.Agents.AI.Workflows.Generators` package generates the handler registration at compile time.

```csharp
[YieldsOutput(typeof(string))]
internal sealed partial class Decision(string id, string label) : Executor(id)
{
    [MessageHandler]
    private async ValueTask HandleAsync(Invoice invoice, IWorkflowContext context) =>
        await context.YieldOutputAsync($"{invoice.Number}: {label} ({invoice.Amount:F2})");
}
```

```csharp
var routing = new WorkflowBuilder(parse)
    .AddEdge<Invoice>(parse, classify, condition: invoice => invoice is { Amount: > 0 })
    .AddEdge<Invoice>(parse, reject, condition: invoice => invoice is { Amount: <= 0 })
    .AddSwitch(classify, switchBuilder => switchBuilder
        .AddCase<Invoice>(invoice => invoice!.Amount < 1000, autoApprove)
        .AddCase<Invoice>(invoice => invoice!.Amount < 10000, managerReview)
        .WithDefault(directorReview))
    .WithOutputFrom(reject, autoApprove, managerReview, directorReview)
    .Build();
```

```bash
cd ../workflows-cs
dotnet run
```

Expected output (Mermaid text trimmed):

```text
INV-001: auto-approved (420.00)
INV-002: sent to the manager (2750.50)
INV-003: sent to the director (18400.00)
INV-004: rejected, amount is not valid (-1.00)
superstep 0
  invoked parse_invoice
superstep 1
  invoked duplicate_check
  invoked budget_check
  invoked vendor_check
superstep 2
  invoked aggregate
  invoked aggregate
  invoked aggregate
output: failed: budget
total 1.0s for three one-second checks
flowchart TD
...
```

The C# fan-in barrier delivers the three checks as separate messages in one superstep, so `Aggregate` keeps a list and yields once it has all three. Superstep numbering starts at 0 in .NET and at 1 in Python.

## Verify

- `routing.py` prints four lines, one per path, and the INV-004 line says rejected.
- `fanout.py` reports a total of about 1.0 seconds, not 3.0.
- `output/invoice-routing.mmd` exists and contains `conditional` edges.
- `dotnet run` prints the same four routing results as Python.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `AgentExecutor 'invoice_reviewer': from_str handler invoked with an empty cache` | An upstream executor sent a plain `str` to an agent | Send an `AgentExecutorRequest` as in Step 4 |
| C# build error `does not implement inherited abstract member 'Executor.ConfigureProtocol'` | The source generator package is missing | Add `Microsoft.Agents.AI.Workflows.Generators` to the `.csproj` |
| C# run ends without output and an `ExecutorFailedEvent` says `Cannot output object of type String` | The executor yields output but does not declare it | Add `[YieldsOutput(typeof(string))]` to the class |
| `DefaultAzureCredential failed to retrieve a token` | No signed-in Azure session | Run `az login` and retry |

## Cleanup

The Python and C# routing and fan-out steps create nothing in Azure. Step 4 only sends model requests. Remove the local render output with `rm -r output` in `workflows-py`.

## Next

Pause a graph at a human decision and resume it from a checkpoint in [Checkpointing & Human-in-the-Loop Requests](../02-checkpoints-hitl/readme.md).
