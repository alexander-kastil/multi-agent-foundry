# Coordinate a Team with Group Chat and a Magentic Manager

Pre-built pipelines are fine when you know the steps. Planning an event, answering an open research question or iterating on copy has no fixed route, and hard-coding one produces either rigid flows or a pile of if statements. In this demo you let a group chat orchestrator choose speakers, then hand a planning task to a Magentic manager that writes a plan, asks you to review it, revises it and tracks progress round by round.

What you practice here: choosing between a selection function, an agent orchestrator and a Magentic manager, and reading the manager's events.

Duration: about 40 minutes.

## Prerequisites

- Python 3.12, or the .NET 10 SDK for the C# project
- Signed in with `az login` and the **Azure AI User** role on the Foundry project
- A chat model deployment, for example `gpt-5.4`, in the Foundry project
- The orchestration builders from [Sequential, Concurrent & Handoff Orchestration](../03-orchestration-patterns/readme.md)

## Run it

```bash
cd demos/05-orchestration/04-magentic/magentic-py
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python group_chat.py
```

The C# project in `magentic-cs/` has one command per script:

```bash
cd demos/05-orchestration/04-magentic/magentic-cs
cp .env.example .env
dotnet run -- groupchat
dotnet run -- magentic
dotnet run -- magentic responses
```

On macOS and Linux use `.venv/bin/python`. Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first.

## Step 1: Create agents on two client types

`agents.py` offers two factories: `responses_agent` uses `FoundryChatClient`, which talks to the Responses API and suits group chat, and `chat_completions_agent` uses `OpenAIChatCompletionClient` against the project's OpenAI v1 endpoint, which Magentic needs on Foundry. Both authenticate with `DefaultAzureCredential` and create a new client per agent.

```python
def chat_completions_agent(name: str, instructions: str, description: str | None = None, tools=None):
    host = urlparse(os.environ["PROJECT_ENDPOINT"]).netloc
    client = OpenAIChatCompletionClient(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        base_url=f"https://{host}/openai/v1/",
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions, description=description, tools=tools or [])
```

In C#, `Agents.cs` offers the same two factories. `ResponsesAgent` calls `AIProjectClient.AsAIAgent`, and `ChatCompletionsAgent` builds an `OpenAIClient` on the project's `/openai/v1/` endpoint with a `BearerTokenPolicy` and calls `GetChatClient(model).AsAIAgent(...)`.

```csharp
OpenAIClient client = new(
    new BearerTokenPolicy(Credential, "https://ai.azure.com/.default"),
    new OpenAIClientOptions { Endpoint = new Uri($"https://{new Uri(Endpoint).Host}/openai/v1/") });
return client.GetChatClient(Model).AsAIAgent(instructions: instructions, name: name, description: description, tools: tools);
```

## Step 2: Run a group chat with a selection function

A writer and a reviewer take turns. The selection function is plain Python that maps the current round to a participant, and the termination condition stops after four assistant messages.

```python
def round_robin(state: GroupChatState) -> str:
    names = list(state.participants.keys())
    return names[state.current_round % len(names)]


selector_chat = GroupChatBuilder(
    name="slogan_round_robin",
    participants=[writer, reviewer],
    selection_func=round_robin,
    termination_condition=lambda conversation: sum(1 for m in conversation if m.role == "assistant") >= 4,
    intermediate_output_from=[writer, reviewer],
).build()
```

```csharp
Workflow selectorChat = AgentWorkflowBuilder
    .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 4 })
    .AddParticipants(writer, reviewer)
    .Build();
```

In .NET the selection function is a `GroupChatManager`; `RoundRobinGroupChatManager` is the built-in one, and `MaximumIterationCount` plays the role of the termination condition.

Run `.venv/Scripts/python group_chat.py` or `dotnet run -- groupchat`. The script prints this section first.

Expected output (agent wording varies):

```text
Selection function, four turns
[Writer] Snap receipts, not your budget.
[Reviewer] APPROVED: Snap receipts, not your budget.
[Writer] Capture receipts in a snap and keep every expense on track.
[Reviewer] APPROVED: Capture receipts in a snap and keep every expense on track.
[final] The group chat has reached its termination condition.
```

The reviewer approved the first slogan, but the writer went on because the selection function only counts rounds. You own the stopping rule when you pick speakers yourself.

## Step 3: Let an orchestrator agent choose

Swap the selection function for `orchestrator_agent`. A model now reads the conversation, picks the next speaker and can end the chat. The instructions tell it when to finish.

```python
orchestrator = responses_agent(
    "Orchestrator",
    "You coordinate a slogan team. Let Writer go first, then Reviewer. Finish as soon as Reviewer says APPROVED.",
    "Selects the next speaker",
)
agent_chat = GroupChatBuilder(
    name="slogan_orchestrated",
    participants=[writer, reviewer],
    orchestrator_agent=orchestrator,
    termination_condition=lambda conversation: sum(1 for m in conversation if m.role == "assistant") >= 6,
    intermediate_output_from=[writer, reviewer],
).build()
```

.NET has no built-in orchestrator agent for group chat, so `GroupChat.cs` implements the same idea as a `GroupChatManager` subclass that asks an orchestrator agent for the next speaker and for `FINISH`.

```csharp
protected override async ValueTask<AIAgent> SelectNextAgentAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
{
    string next = decision ?? await Decide(history, cancellationToken);
    decision = null;
    return agents.FirstOrDefault(agent => string.Equals(agent.Name, next, StringComparison.OrdinalIgnoreCase)) ?? agents[0];
}
```

The second half of the same run prints this section.

Expected output:

```text
Agent orchestrator picks the speaker
[Writer] Snap receipts, track spending, stay effortlessly on budget.
[Reviewer] APPROVED: Snap receipts, track spending, stay effortlessly on budget.
[final] Snap receipts, track spending, stay effortlessly on budget.
```

Two turns instead of four, and the final output is the approved slogan rather than a termination notice. The termination condition is still the safety net.

## Step 4: Build a Magentic team with a manager and a tool

Magentic needs a manager agent and participants with clear descriptions, because the manager reads those descriptions when it picks speakers. The analyst owns a `sum_costs` tool so that the arithmetic is not left to the model. The loop limits are set explicitly.

```python
@tool(approval_mode="never_require")
def sum_costs(items: Annotated[list[float], "Cost of each line item in EUR"]) -> str:
    """Adds up line item costs and returns the total in EUR."""
    return f"{sum(items):.2f} EUR"


researcher = make_agent("Researcher", "You propose venue, catering and agenda options with plausible prices. You do not add up costs.", "Finds options and prices")
analyst = make_agent("Analyst", "You verify budgets. Always call sum_costs for the total.", "Checks totals against the budget", tools=[sum_costs])
manager = make_agent("Manager", "You coordinate the team to complete the task efficiently.", "Plans and delegates")
workflow = MagenticBuilder(
    name="offsite_planning",
    participants=[researcher, analyst],
    manager_agent=manager,
    enable_plan_review=enable_plan_review,
    max_round_count=8,
    max_stall_count=2,
    max_reset_count=1,
).build()
```

The C# builder takes the manager first and the team second, and plan sign-off is a method instead of a flag.

```csharp
[Description("Adds up line item costs and returns the total in EUR.")]
static string SumCosts([Description("Cost of each line item in EUR")] double[] items) => $"{items.Sum():F2} EUR";

Workflow workflow = AgentWorkflowBuilder
    .CreateMagenticBuilderWith(manager)
    .AddParticipants(researcher, analyst)
    .WithMaxRounds(12)
    .WithMaxStalls(2)
    .WithMaxResets(1)
    .RequirePlanSignoff(enablePlanReview)
    .Build();
```

The C# run uses `WithMaxRounds(12)` instead of 8, because the extra plan reviews after a stall consume rounds and a budget of 8 ended two of three trial runs with "maximum round count limit".

The task is a single message: plan a one-day offsite in Vienna for 12 people with a 1500 EUR budget, including venue, catering and a four-item agenda, and verify the total.

## Step 5: Review the plan and read the orchestrator events

With `enable_plan_review=True` the first run ends at a `request_info` event that carries the manager's plan. The driver revises the plan once and approves the second one. Every `magentic_orchestrator` event is printed with its type.

```python
async for event in stream:
    if event.type == "magentic_orchestrator":
        print(f"[{event.data.event_type.name}] {describe(event)}")
    elif event.type == "request_info" and event.request_type is MagenticPlanReviewRequest:
        review = event
```

```python
if revisions == 0:
    revisions += 1
    responses = {review.request_id: request.revise(FEEDBACK)}
else:
    responses = {review.request_id: request.approve()}
```

```csharp
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, Task));
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is MagenticOrchestratorEvent)
        Console.WriteLine(Describe(evt));
    else if (evt is RequestInfoEvent info && info.Request.TryGetDataAs(out MagenticPlanReviewRequest? request))
    {
        MagenticPlanReviewResponse response = revisions++ == 0 ? request.Revise(Feedback) : request.Approve();
        await run.SendResponseAsync(info.Request.CreateResponse(response));
    }
    else if (evt is WorkflowOutputEvent output and not AgentResponseUpdateEvent)
        return output.Data switch { ChatMessage message => message.Text, _ => output.Data?.ToString() ?? "" };
}
```

Two .NET details matter. The run produces no events until a `TurnToken` is sent, and `AgentResponseUpdateEvent` derives from `WorkflowOutputEvent`, so the final-output branch must exclude it or the loop returns at the first streamed token.

Run `.venv/Scripts/python magentic.py` or `dotnet run -- magentic`. It takes a few minutes because the manager plans, replans and runs several rounds.

Expected output (the report text is model output and varies):

```text
Magentic on a Chat Completions client
[PLAN_CREATED] 3873 characters, mentions 400 EUR: False
plan under review, revision 0: stalled=False
[REPLANNED] 6497 characters, mentions 400 EUR: True
plan under review, revision 1: stalled=False
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Researcher
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Analyst
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Analyst
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Researcher
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Analyst
[PROGRESS_LEDGER_UPDATED] satisfied=True next=Analyst
Here’s a one-day team offsite plan in Vienna for 12 people that stays within your €1,500 budget and keeps the venue under €400.

## Venue
...
```

A trimmed C# run:

```text
Magentic on a Chat Completions client
[PLAN_CREATED] 4034 characters, mentions 400 EUR: True
plan under review, revision 0: stalled=False
[REPLANNED] 6750 characters, mentions 400 EUR: True
plan under review, revision 1: stalled=False
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Researcher
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Analyst
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Researcher
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Researcher
[PROGRESS_LEDGER_UPDATED] satisfied=False next=Analyst
[PROGRESS_LEDGER_UPDATED] satisfied=True next=Analyst
Here's a practical one-day team offsite plan in Vienna for 12 people that stays well within your 1,500 EUR budget.
...
- **Total:** **738.87 EUR**
```

In this run the first plan already mentioned the 400 EUR limit, so the planner wording is model output and the flags vary between runs. Read it in order. `PLAN_CREATED` is the initial task ledger, and your feedback produced `REPLANNED`, whose new plan text mentions the 400 EUR limit that the first one did not. Each `PROGRESS_LEDGER_UPDATED` is one round: the manager says who speaks next, and `satisfied=True` ends the loop before the final report.

## Step 6: Confirm why Magentic needs the Chat Completions client

The flag `responses` runs the same team on `FoundryChatClient`. The point is to see the failure once, so you recognise it later.

```bash
.venv/Scripts/python magentic.py responses
```

This failure is specific to the Python Responses client. The .NET `ResponsesAgent` completed the same team in two trial runs (`dotnet run -- magentic responses`), reaching either `satisfied=True` or the reset limit, so there is no .NET equivalent of this error to show.

Expected output of the Python run (end of the traceback):

```text
agent_framework.exceptions.ChatClientException: ("<class 'agent_framework_foundry._chat_client.FoundryChatClient'> service failed to complete the prompt: Error code: 400 - {'error': {'message': 'No tool call found for function call output with call_id call_u0c...
```

The manager's internal function call does not round-trip through the Responses API. Rebuild every participant and the manager with `chat_completions_agent` and the error disappears.

## Verify

- `group_chat.py` prints the agent-orchestrator section with fewer turns than the selection-function section.
- `magentic.py` prints `PLAN_CREATED`, then `REPLANNED` after your feedback, and at least one `PROGRESS_LEDGER_UPDATED` line.
- The `REPLANNED` line reports `mentions 400 EUR: True`.
- `magentic.py responses` fails with `No tool call found for function call output`. The C# project does not fail here; see Step 6.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `ValueError: Magentic only support a single task message to start the workflow.` | More than one message was passed to `run` | Pass the task as one string |
| `DefaultAzureCredential failed to retrieve a token` | No signed-in Azure session | Run `az login` and retry |
| The plan review never appears | `enable_plan_review` is `False`, the default in Python | Set `enable_plan_review=True` on `MagenticBuilder` |

## Cleanup

Nothing is created in Azure. Agents are in-memory objects in both projects.

## Next

Decompose a task into a dependency graph that a planner agent builds at run time in [Task Decomposition: Planner & Waves](../05-task-decomposition/readme.md).
