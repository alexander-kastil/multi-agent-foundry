# Pause an Expense Approval and Resume It in Another Process

An approval workflow that keeps its state in memory breaks the first time the server restarts or the approver answers the next morning. You build an expense workflow where an agent summarizes the report, the workflow stops and asks a person, the process exits, and a different process picks up the saved checkpoint, answers the question and finishes. You then rewind to an earlier superstep and see what a replay costs.

What you practice here: pairing `ctx.request_info` with `FileCheckpointStorage` so a human decision can arrive in a separate process.

Duration: about 25 minutes.

## Prerequisites

- Python 3.12 and the .NET 10 SDK (for the C# step)
- Signed in with `az login` and the **Azure AI User** role on the Foundry project
- A chat model deployment, for example `gpt-5.4`, in the Foundry project
- The concepts from [Workflows: Executors, Edges & Supersteps](../01-workflows/readme.md)

## Run it

```bash
cd demos/05-orchestration/02-checkpoints-hitl/checkpoints-hitl-py
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python submit.py
```

On macOS and Linux use `.venv/bin/python`. Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first.

## Step 1: Define the request and the approval gate

The request payload is a dataclass, and the gate is an executor with two methods. `ask` runs after the agent and raises the request. `decide` is the `@response_handler` the framework calls when the answer arrives, and its `original_request` parameter is the same `ApprovalRequest` the gate sent.

```python
@dataclass
class ApprovalRequest:
    summary: str


class ApprovalGate(Executor):
    def __init__(self) -> None:
        super().__init__(id="approval_gate")
        self.decisions = 0

    @handler
    async def ask(self, response: AgentExecutorResponse, ctx: WorkflowContext) -> None:
        await ctx.request_info(
            request_data=ApprovalRequest(summary=response.agent_response.text),
            response_type=str,
        )

    @response_handler
    async def decide(self, original_request: ApprovalRequest, decision: str, ctx: WorkflowContext[Never, str]) -> None:
        self.decisions += 1
        await ctx.yield_output(f"decision {self.decisions}: {decision.upper()} for: {original_request.summary}")
```

The `decisions` counter is state that the framework cannot see. Two small methods persist it into every checkpoint.

```python
    async def on_checkpoint_save(self) -> dict[str, Any]:
        return {"decisions": self.decisions}

    async def on_checkpoint_restore(self, state: dict[str, Any]) -> None:
        self.decisions = state.get("decisions", 0)
```

## Step 2: Attach file checkpoint storage

The storage is one constructor argument on the builder. `allowed_checkpoint_types` lists the application type that appears in a checkpoint; without it the checkpoint holding `ApprovalRequest` could not be decoded again.

```python
storage = FileCheckpointStorage(CHECKPOINT_DIR, allowed_checkpoint_types=["expense:ApprovalRequest"])
workflow = (
    WorkflowBuilder(
        name="expense_approval",
        start_executor=intake,
        output_from=[gate],
        checkpoint_storage=storage,
    )
    .add_edge(intake, summarizer)
    .add_edge(summarizer, gate)
    .build()
)
```

`submit.py` runs this workflow with a 640 EUR dinner receipt and stops where the workflow stops. Run `.venv/Scripts/python submit.py`.

Expected output (the summary wording is model output and varies):

```text
waiting for a human, request 97a8ecb2-562e-4948-ac9f-4673598a9915
summary: A team offsite dinner for 12 people was purchased at Gasthaus Zum Goldenen Hirschen for a total of 640 EUR. At about 53 EUR per person, the amount looks reasonable for a group dinner.
4 checkpoints on disk, the process now exits
```

The python process is gone and nothing is waiting in memory. The decision now lives only in the `checkpoints` folder.

## Step 3: Read what is on disk

A checkpoint is a record of one superstep. `checkpoints.py` lists them for the workflow name and shows which ones still hold an unanswered request.

```python
checkpoints = await storage.list_checkpoints(workflow_name="expense_approval")
for checkpoint in sorted(checkpoints, key=lambda c: c.timestamp):
    pending = list(checkpoint.pending_request_info_events)
    print(f"{checkpoint.checkpoint_id[:8]}  superstep={checkpoint.iteration_count}  pending_requests={len(pending)}")
```

Run `.venv/Scripts/python checkpoints.py`.

Expected output (ids differ on every run):

```text
eebeed27  superstep=0  pending_requests=0
043d107d  superstep=1  pending_requests=0
df9d92a4  superstep=2  pending_requests=0
3533abcf  superstep=3  pending_requests=1
```

Superstep 0 is the entry checkpoint with the workflow input. Superstep 3 is the pause: the summarizer finished and the gate has an open request.

## Step 4: Resume in a new process and answer

`decide.py` is a different process with a freshly built workflow object. It loads the latest checkpoint, resumes from it to collect the re-emitted request, and then sends the human answer with `responses=`.

```python
chosen = await storage.get_latest(workflow_name=workflow.name)
pending = [
    event
    async for event in workflow.run(checkpoint_id=chosen.checkpoint_id, stream=True)
    if event.type == "request_info"
]
async for event in workflow.run(responses={pending[0].request_id: decision}, stream=True):
    if event.type == "output":
        print(event.data)
```

Run `.venv/Scripts/python decide.py approve`.

Expected output:

```text
resuming from checkpoint 3533abcf (superstep 3)
re-emitted request: 97a8ecb2-562e-4948-ac9f-4673598a9915
decision 1: APPROVE for: A team offsite dinner for 12 people was purchased at Gasthaus Zum Goldenen Hirschen for a total of 640 EUR. At about 53 EUR per person, the amount looks reasonable for a group dinner.
```

The request id is the one `submit.py` printed, and the agent did not run again. The summary in the decision is the one stored in the checkpoint.

Run `.venv/Scripts/python checkpoints.py` once more to see the new checkpoints that the resume and the answer added.

```text
eebeed27  superstep=0  pending_requests=0
043d107d  superstep=1  pending_requests=0
df9d92a4  superstep=2  pending_requests=0
3533abcf  superstep=3  pending_requests=1
f8d2fd4e  superstep=4  pending_requests=1
fc07038b  superstep=4  pending_requests=0
74b3db70  superstep=5  pending_requests=0
```

## Step 5: Rewind to an earlier superstep

Any checkpoint is a valid start point. Resuming from superstep 1 starts before the summarizer ran, so the agent runs again and the gate raises a new request with a new id. Run `.venv/Scripts/python decide.py reject 1`.

Expected output:

```text
resuming from checkpoint 043d107d (superstep 1)
re-emitted request: 4e344c49-f90a-41dc-aeb7-fc71217337ed
decision 1: REJECT for: A team offsite dinner for 12 people was purchased at Gasthaus Zum Goldenen Hirschen, totaling 640 EUR. That amount looks normal for a group dinner, at about 53 EUR per person.
```

The summary wording is different because the model ran again, and the request id is new. Use a rewind to recover from a bad step, and expect agent calls after the chosen superstep to be repeated and billed.

## Step 6: The same pause with a RequestPort in C#

The .NET version uses a `RequestPort` node instead of `ctx.request_info`. The first run ends as soon as the request is pending because `blockOnPendingRequest: false` stops the stream. The second half builds a brand new workflow, resumes it from the saved checkpoint and answers through `SendResponseAsync`.

```csharp
var approvalPort = RequestPort.Create<ApprovalRequest, string>("approval");
var workflow = new WorkflowBuilder(intake)
    .AddEdge(intake, approvalPort)
    .AddEdge(approvalPort, finalize)
    .WithOutputFrom(finalize)
    .Build();
```

```csharp
await using StreamingRun resumed = await InProcessExecution.ResumeStreamingAsync(BuildWorkflow(), waiting!, checkpointManager);
await foreach (WorkflowEvent evt in resumed.WatchStreamAsync())
{
    if (evt is RequestInfoEvent request)
    {
        await resumed.SendResponseAsync(request.Request.CreateResponse("approve"));
    }
}
```

```bash
cd ../checkpoints-hitl-cs
dotnet run
```

Expected output:

```text
superstep 0 checkpoint c13a04dd
waiting for a human, request 799707e556cd43fc9323ed67c5148381
superstep 1 checkpoint 143edd75
the first run is gone, resuming into a new workflow instance
re-emitted request 799707e556cd43fc9323ed67c5148381
decision: APPROVE
```

## Verify

- `checkpoints.py` shows exactly one checkpoint with `pending_requests=1` after `submit.py`.
- `decide.py approve` prints the same request id that `submit.py` printed.
- `decide.py reject 1` prints a different request id and a different summary text.
- The C# run prints the same request id before and after the resume.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `decide.py` fails with `IndexError` because no request was re-emitted | `ApprovalRequest` is missing from `allowed_checkpoint_types`, so the checkpoint cannot be decoded and is skipped | List the type as `"module:QualName"` on the storage that reads and writes |
| Log line `Failed to create checkpoint at iteration 2: No such file or directory` | The checkpoint folder was deleted while the run was starting | Remove the folder only between runs, not during one |
| C# `dotnet run` never returns after the first run | `WatchStreamAsync` waits for a response to the pending request | Pass `blockOnPendingRequest: false` for the run that should end at the pause |
| Resume fails after you changed an executor id | A rehydrated workflow must keep the topology and executor ids of the checkpoint | Keep ids stable and start a new checkpoint lineage when you change the graph |

## Cleanup

```bash
rm -r checkpoints
cd ../checkpoints-hitl-cs
rm -r checkpoints
```

Nothing is created in Azure; the agent only sends model requests.

## Next

Combine agents into ready-made patterns with [Sequential, Concurrent & Handoff Orchestration](../03-orchestration-patterns/readme.md).
