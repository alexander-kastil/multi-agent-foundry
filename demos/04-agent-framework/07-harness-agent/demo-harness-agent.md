# Build a Plan-Then-Execute Trek Planner with a Harness Agent

A multi-day trek needs checks, reservations and a record of what is done, and a plain agent forgets the plan, asks for permission on every booking and fills its context window with tool output. In this demo you build a trek planner on a harness agent that plans before it acts, books under a single standing approval, loops until its todo list is empty, compacts its history and remembers the group between processes. You finish with one agent that handles a long task the way a person would.

What you practice here: running a long task with create_harness_agent.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later, the Azure CLI signed in with `az login`, and a Foundry project with a `gpt-5.4` deployment.
- For the C# step: .NET 10 SDK.
- Several scripts print an `ExperimentalWarning`. The harness itself is released; looping, file stores and compaction are marked experimental.

## Run it

```powershell
cd harness-agent-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env`. The finished projects are `harness-agent-py/` and `harness-agent-cs/`.

## Step 1: Create the harness agent and plan first

`agent_factory.py` calls `create_harness_agent` with two trek tools and turns off web search and file memory for now. `get_stage_status` reads a stage; `reserve_hut` is marked `always_require` because it commits the customer. `store=False` keeps the history local so compaction works later.

```python
def build_harness(**overrides) -> Agent:
    options = dict(
        client=build_client(),
        name="TrekPlanner",
        agent_instructions=INSTRUCTIONS,
        tools=[get_stage_status, reserve_hut],
        disable_web_search=True,
        disable_file_memory=True,
        default_options={"store": False},
    )
    options.update(overrides)
    return create_harness_agent(**options)
```

`run_plan_execute.py` sends the first request while the agent is in its default `plan` mode, then prints the mode and the todo list kept in the session.

```python
result = await agent.run(PLAN_REQUEST, session=session)
print(result.messages[-1].text)
print(f"\nmode: {get_agent_mode(session)}")
show_todos(session)
```

```powershell
python run_plan_execute.py
```

Expected output of the plan phase:

```text
I checked all four stages. Stage 1 is open at 5 hours with 900 m ascent, Stage 2 is open at 4 hours with snow above 2600 m, Stage 3 is open at 6 hours with an exposed ridge, and Stage 4 is closed due to a rockfall warning. I can proceed to reserve huts for 3 hikers on the open stages, which would mean Elberfelder Hut, Salm Hut, and Glocknerhaus. Please confirm if you want me to switch to execution and make those reservations.

mode: plan
  [x] 1. Check status of all 4 Grossglockner stages
  [x] 2. Identify huts to reserve for open stages
  [ ] 3. Present plan and request approval to proceed
```

The agent read the stages, wrote its own todos and stopped at the booking, as the plan mode requires. Nothing was reserved yet. The wording of the todos differs on every run.

## Step 2: Execute with one standing approval

The application switches the session to `execute`, which tells the agent it may act. `reserve_hut` still needs approval, and the script answers the first request with `create_always_approve_tool_response`, which records a standing rule in the session.

```python
set_agent_mode(session, "execute")

result = await agent.run("The plan is approved. Execute it, including the hut reservations.", session=session)
while result.user_input_requests:
    responses = []
    for request in result.user_input_requests:
        call = request.function_call
        print(f"approval requested: {call.name}({call.arguments})")
        responses.append(Message(role="user", contents=[create_always_approve_tool_response(request)]))
    result = await agent.run(responses, session=session)
```

The same run continues with the output below.

```text
mode: execute
approval requested: reserve_hut({"hut":"Elberfelder Hut","persons":3})
Done. I reserved 3 beds each at Elberfelder Hut, Salm Hut, and Glocknerhaus. Your 4 stage trek includes 3 open stages with bookings in place, while Stage 4 Glocknerhaus to Kals remains closed due to a rockfall warning.

  [x] 1. Check status of all 4 Grossglockner stages
  [x] 2. Identify huts to reserve for open stages
  [x] 3. Present plan and request approval to proceed
reservations executed: ['Elberfelder Hut x3', 'Salm Hut x3', 'Glocknerhaus x3']
```

One approval request produced three reservations. The standing rule approved the second and third call without asking, and every todo is complete.

## Step 3: Loop until the todo list is empty

Some models stop after one step. `run_loop.py` tells the agent to finish exactly one todo per reply, then compares a plain run with a run that has the experimental todo loop.

```python
agent = build_harness(
    tools=[get_stage_status],
    agent_instructions=STEP_BY_STEP,
    loop_should_continue=todos_remaining(looping_modes=["execute"]),
    loop_next_message=todos_remaining_message,
    loop_max_iterations=6,
)
```

```powershell
python run_loop.py
```

Expected output:

```text
ExperimentalWarning: [HARNESS] create_harness_agent parameter(s) 'loop_should_continue' enable experimental harness features that may change or be removed in future versions without notice.
without loop: 1 of 4 todos complete
with loop   : 4 of 4 todos complete
```

The loop re-invokes the agent with a reminder of the open todos until none are left or six iterations pass. The bound is the safety net, so always set it.

## Step 4: Compact the history

`get_stage_report` returns a long hourly report, so six questions fill the context fast. `run_compaction.py` runs the same six questions twice and prints the input tokens of each turn.

```python
await run("no compaction  ", disable_compaction=True)
await run("with compaction", max_context_window_tokens=4000, max_output_tokens=500)
```

```powershell
python run_compaction.py
```

Expected output:

```text
no compaction  : [961, 1877, 2819, 3760, 4701, 5639]
with compaction: [961, 1875, 2814, 3753, 4678, 2888]
```

Without compaction each turn re-sends everything and the count grows by about 940 tokens per question. With a 4000 token window the harness compacts when the history approaches the limit, and the last turn falls from about 5600 to 2888 tokens. During the compaction run this version logs `Rejected N compaction summary message(s) because their dependencies were not fully rooted ...` warnings on stderr; the token counts show that compaction still took effect.

## Step 5: Remember the group in file memory

File memory is on by default; the first agent in `run_file_memory.py` writes notes into a folder, then a second agent with an empty history reuses the same session ID.

```python
def build():
    return build_harness(
        disable_file_memory=False,
        file_memory_store=FileSystemAgentFileStore(MEMORY_ROOT),
        disable_todo=True,
        disable_mode=True,
    )

first = build()
await first.run("Remember for later: our group of 3 uses trekking poles and avoids exposed ridges.", session=AgentSession(session_id=SESSION_ID))

second = build()
await second.run("Which stage should we skip, given what you know about our group?", session=AgentSession(session_id=SESSION_ID))
```

```powershell
python run_file_memory.py
```

Expected output:

```text
Got it. I've saved that your group has 3 people, uses trekking poles, and avoids exposed ridges.
file: agent-file-memory/grossglockner-group/memories.md
# Memory Index

- **userpreferences.md**: User trekking group preferences for this session, including group size and route considerations.
file: agent-file-memory/grossglockner-group/userpreferences.md
Group size: 3
Preferences: uses trekking poles, avoids exposed ridges.
file: agent-file-memory/grossglockner-group/userpreferences_description.md
User trekking group preferences for this session, including group size and route considerations.
Based on what I know, Stage 1 Heiligenblut to Elberfelder Hut is the only stage I could verify, ... Since your group avoids exposed ridges, the stage to skip would most likely be whichever stage has the most exposed ridge travel, ...
```

The second agent never saw the first message, yet its answer uses the preference. The notes live under the session ID folder, which is why a different session ID would start empty.

## Step 6: Build the same planner in C#

`AsHarnessAgent` comes from the `Microsoft.Agents.AI.Harness` package. The C# project builds an `IChatClient` from the project client, sets the context window for compaction, and wraps the booking function in `ApprovalRequiredAIFunction`.

```csharp
IChatClient chatClient = projectClient
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(model);

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    Name = "TrekPlanner",
    ChatOptions = new ChatOptions { Instructions = "...", Tools = [..., new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ReserveHut, "ReserveHut"))] },
    DisableWebSearch = true,
    DisableFileMemory = true,
    MaxContextWindowTokens = 128_000,
    MaxOutputTokens = 16_384,
});
```

```powershell
cd ../harness-agent-cs
Copy-Item .env.example .env
dotnet run
```

Expected output:

```text
Plan for 3 hikers: Stage 1 is open at 5 h with 900 m ascent to Elberfelder Hut, Stage 2 is open at 4 h with snow above 2600 m to Salm Hut, Stage 3 is open at 6 h with an exposed ridge to Glocknerhaus, and Stage 4 to Kals is closed due to rockfall warning.  
Proposed reservations for the open stages are Elberfelder Hut, Salm Hut, and Glocknerhaus for 3 people each.  
If you approve, I will switch to execute mode and make those three reservations.
mode: plan
approval requested: ReserveHut(hut=Elberfelder Hut, persons=3)
approval requested: ReserveHut(hut=Salm Hut, persons=3)
approval requested: ReserveHut(hut=Glocknerhaus, persons=3)
Done. I switched to execute mode and reserved 3 beds each at Elberfelder Hut, Salm Hut, and Glocknerhaus.
Stage 4 Glocknerhaus to Kals remains closed due to rockfall warning, so no reservation was needed for that stage.
```

The C# loop answers each of the three requests separately because it sets no standing rule. Here the model switches to execute mode itself after the approving prompt. Calling `SetModeAsync` from the application was tried instead, and on this deployment the next run failed with an HTTP 400 `content_filter` error in two attempts, so the demo does not use it.

## Verify

Step 2 ends with three reservations after a single approval request, Step 3 reports `4 of 4 todos complete` with the loop, and Step 4 shows a lower last-turn token count with compaction. Confirm that the memory notes are on disk.

```powershell
Get-ChildItem agent-file-memory/grossglockner-group -Name
```

Expected output:

```text
memories.md
userpreferences.md
userpreferences_description.md
```

## Cleanup

Nothing was created in Azure.

```powershell
Remove-Item -Recurse -Force agent-file-memory
deactivate
Remove-Item -Recurse -Force .venv
```
