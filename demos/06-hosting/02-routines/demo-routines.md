# Schedule an Agent with Routines

A support digest that someone has to remember to request is a digest that stops arriving. You could wire a timer, a queue and an auth layer around the agent, or you could tell the project when the agent should run. In this demo you create a prompt agent, give it a weekday schedule and a one-time timer with routines, trigger a test run on demand, create the same schedule from an azd manifest, and read the run history that Foundry keeps.

What you practice here: pairing one trigger with one action, testing with `dispatch`, and reading run history.

Duration: about 40 minutes.

## Prerequisites

- Python 3.10 or later, or the .NET 10 SDK for the C# version, Azure CLI signed in with `az login`, and the Azure Developer CLI 1.25.2 or later.
- Foundry User on the `pro-code-agents` project and a `gpt-5.4` deployment.
- A project in a region that offers routines. Sweden Central does.

Install the azd extensions once; `microsoft.foundry` bundles `azure.ai.routines`.

```bash
azd ext install microsoft.foundry
```

## Run it

Python works in `routines-py/`, C# in `routines-cs/`. Both need the same three values in `.env`.

```bash
cd demos/06-hosting/02-routines/routines-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
AGENT_NAME=m6-digest-agent
```

For C#, copy `.env.example` to `.env` in `routines-cs/` and restore the packages. The C# project reads the same variables and takes a command as its first argument:

```bash
cd demos/06-hosting/02-routines/routines-cs
cp .env.example .env
dotnet run -- agent
```

The .NET routines client exists only in the `3.0.0-beta` packages, so `routines-cs.csproj` pins `Azure.AI.Projects`, `Azure.AI.Projects.Agents` and `Azure.AI.Extensions.OpenAI` to `3.0.0-beta.5`. The stable 2.0.1 packages have no routines API, so every `routines-cs` snippet below uses the SDK (`project.Routines`) and none falls back to REST.

## Step 1: Create the agent the routine will call

A routine invokes an agent that already exists. `create_agent.py` creates a prompt agent version; routines accept prompt agents and hosted agents, not workflow agents.

```python
agent = project.agents.create_version(
    agent_name=os.environ["AGENT_NAME"],
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions=(
            "You write the Contoso support desk digest. "
            "Given a request, answer with a three line digest: open tickets, risks, next action."
        ),
    ),
)
```

```csharp
ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync(
    agentName: agentName,
    options: new(new DeclarativeAgentDefinition(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
    {
        Instructions = "You write the Contoso support desk digest. Given a request, answer with a three line digest: open tickets, risks, next action.",
    }));
```

```bash
.venv/Scripts/python create_agent.py
dotnet run -- agent
```

Expected output (Python, then C#):

```text
agent m6-digest-agent version 1
agent m6h3-digest-agent version 1
```

## Step 2: Create a schedule and a timer

`create_routines.py` creates two routines. The schedule fires on weekdays at 07:00 Vienna time and is created disabled, so nothing runs unattended. The timer fires once, two minutes from now.

```python
schedule = project.beta.routines.create_or_update(
    routine_name="m6-weekday-digest",
    enabled=False,
    triggers={
        "weekday-morning": {
            "type": "schedule",
            "cron_expression": "0 7 * * 1-5",
            "time_zone": "Europe/Vienna",
        }
    },
    action={
        "type": "invoke_agent_responses_api",
        "agent_name": agent_name,
        "input": "Write the support digest for the last 24 hours.",
    },
)

timer = project.beta.routines.create_or_update(
    routine_name="m6-one-shot",
    enabled=True,
    triggers={"once": {"type": "timer", "at": fire_at}},
    action={
        "type": "invoke_agent_responses_api",
        "agent_name": agent_name,
        "input": "Write the one-time support digest.",
    },
)
```

```csharp
ProjectsRoutineOptions scheduleOptions = new(
    action: new AgentResponsesApiRoutineAction
    {
        AgentName = agentName,
        Input = BinaryData.FromObjectAsJson("Write the support digest for the last 24 hours."),
    },
    description: "Weekday morning support digest.",
    enabled: false);
scheduleOptions.Triggers.Add("weekday-morning", new ScheduleRoutineTrigger(cronExpression: "0 7 * * 1-5", timeZone: "Europe/Vienna"));
ProjectsRoutine schedule = await routines.CreateOrUpdateAsync("m6h3-weekday-digest", scheduleOptions);

timerOptions.Triggers.Add("once", new TimerRoutineTrigger { At = DateTimeOffset.UtcNow.AddMinutes(2) });
ProjectsRoutine timer = await routines.CreateOrUpdateAsync("m6h3-one-shot", timerOptions);
```

The C# project names its routines `m6h3-*` so the two versions never collide in the same project.

```bash
.venv/Scripts/python create_routines.py
dotnet run -- routines
```

Expected output (the time differs; Python first, then C#):

```text
routine m6-weekday-digest enabled=False
routine m6-one-shot enabled=True fires at 2026-10-18T07:15:26+00:00
routine m6h3-weekday-digest enabled=False
routine m6h3-one-shot enabled=True fires at 2026-10-18T08:25:23+00:00
```

The `triggers` map holds exactly one entry and its key is only a label. `cron_expression` has five fields, a schedule cannot fire more often than every five minutes, and `at` needs an explicit UTC offset.

## Step 3: Dispatch a test run and read the history

Waiting for 07:00 is no way to test. `dispatch` queues a run now, and `list_runs` shows every run with its trigger source and outcome. The disabled schedule can be dispatched because the request carries an explicit payload.

```python
result = project.beta.routines.dispatch(
    routine_name="m6-weekday-digest",
    payload={"type": "invoke_agent_responses_api", "input": "Run the digest now for a test."},
)
```

```csharp
DispatchRoutineResult result = await routines.DispatchAsync(
    name: "m6h3-weekday-digest",
    payload: new AgentResponsesApiDispatchPayload(BinaryData.FromObjectAsJson("Run the digest now for a test.")));
```

```bash
.venv/Scripts/python run_history.py dispatch
dotnet run -- dispatch
```

Expected output (the ids differ):

```text
dispatch_id: dispatch_f2f45541ea064f3dad3960f0880d2503
m6-one-shot  enabled=True  triggers=['once']
  afe0b466-9b16-4087-8a62-a03d66754188  phase=queued  source=timer_delivery  started=2026-10-18 07:13:28+00:00
    dispatch=dispatch_d0a700bbcd694a12a321dcb6d3564e0a  response=None
m6-weekday-digest  enabled=False  triggers=['weekday-morning']
  409c8678-e60b-4586-89bd-8773c85b0ccb  phase=completed  source=queued_dispatch  started=2026-10-18 07:13:31+00:00
    dispatch=dispatch_f2f45541ea064f3dad3960f0880d2503  response=resp_00c38c5b00c869e9006ad4719b9ed48190994dfcdebe9f3d46
```

The C# output has the same shape (trimmed):

```text
dispatch_id: dispatch_51b007fac72c4cb19e9918894df8d9ea
m6h3-one-shot  enabled=True  triggers=[once]
  4fbafae9-dd38-4220-bc08-36c34c25614d  phase=queued  source=timer_delivery  started=2026-10-18 08:23:26+00:00
    dispatch=dispatch_9e2cf0404c51434895b7574f77a138fd  response=None
m6h3-weekday-digest  enabled=False  triggers=[weekday-morning]
  d76d0a9d-e4db-44c1-bb05-10c4b0ab7626  phase=completed  source=queued_dispatch  started=2026-10-18 08:23:31+00:00
    dispatch=dispatch_51b007fac72c4cb19e9918894df8d9ea  response=resp_0520001755ba1fbc006ad48204832881909a3e691c1f49a189
```

The test run completed within seconds and carries the response id of the agent call. The timer's run is already listed as `queued`, with no response yet, because its fire time has not arrived.

To aim the same routines at a hosted agent, point `AGENT_NAME` at one that exposes the Responses protocol, such as `m6h3-azd-agent` from the next topic, and run `dotnet run -- routines` and `dotnet run -- dispatch` again. The action is identical; only the target differs, and the dispatched run completes with a `caresp_` response id instead of a `resp_` one. Skip `dotnet run -- cleanup` in that case, because it also deletes the agent, and remove the routines with `azd ai routine delete` instead.

## Step 4: Create the same schedule with azd

The azd extension creates a routine from a YAML manifest. The manifest below is the schedule from step 2 with a new name; this version of the extension expects camelCase keys.

```yaml
name: m6-azd-digest
description: Weekday morning support digest created from a manifest.
enabled: false
triggers:
  weekday-morning:
    type: schedule
    cronExpression: "0 7 * * 1-5"
    timeZone: Europe/Vienna
action:
  type: invoke_agent_responses_api
  agentName: m6-digest-agent
  input: Write the support digest for the last 24 hours.
```

Export the project endpoint, create the routine, then enable it, dispatch it and disable it again. A routine that is disabled rejects a dispatch without a payload, so enable it first.

```bash
export PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
azd ai routine create m6-azd-digest --file routine.yaml -p "$PROJECT_ENDPOINT" --no-prompt
azd ai routine enable m6-azd-digest -p "$PROJECT_ENDPOINT" --no-prompt
azd ai routine dispatch m6-azd-digest -p "$PROJECT_ENDPOINT" --no-prompt
azd ai routine disable m6-azd-digest -p "$PROJECT_ENDPOINT" --no-prompt
```

Expected output:

```text
Routine 'm6-azd-digest' created.

Name:                       m6-azd-digest
Description:                Weekday morning support digest created from a manifest.
Enabled:                    false
Dispatch identity:          agent
Trigger (weekday-morning):  schedule
  Cron:                     0 7 * * 1-5
  TimeZone:                 Europe/Vienna
Action:                     invoke_agent_responses_api
  AgentName:                m6-digest-agent
  Input:                    "Write the support digest for the last 24 hours."
Routine 'm6-azd-digest' enabled.
Routine 'm6-azd-digest' dispatched.
Dispatch ID: dispatch_f2aeb9e4bb334f99a32e03f900bd44b8
Routine 'm6-azd-digest' disabled.
```

The C# project ships its own `routine.yaml` with `name: m6h3-azd-digest` and `agentName: m6h3-digest-agent`; the commands are the same, run from `routines-cs/`. The output has the same shape with those names.

Dispatch identity `agent` is the default: the agent runs under its own identity, not yours.

## Step 5: Read the run history after the timer fires

Wait until the two minutes from step 2 have passed, then read the history again. Run history is read through the SDK; `azd ai routine` creates and operates routines but does not list their runs.

```csharp
await foreach (RoutineRun run in routines.GetRoutineRunsAsync(routine.Name))
{
    Console.WriteLine($"  {run.Id}  phase={run.Phase}  source={run.AttemptSource}  started={run.StartedOn:yyyy-MM-dd HH:mm:sszzz}");
    Console.WriteLine($"    dispatch={run.DispatchId}  response={run.ResponseId ?? "None"}");
}
```

```bash
.venv/Scripts/python run_history.py
dotnet run -- history
```

Expected output (the ids differ):

```text
m6-azd-digest  enabled=False  triggers=['weekday-morning']
  a999b8e3-3f0f-421d-8f51-6a42a13d4253  phase=completed  source=schedule_delivery  started=2026-10-18 07:14:10+00:00
    dispatch=dispatch_f2aeb9e4bb334f99a32e03f900bd44b8  response=resp_011bfe6ab6028484006ad471c373c08194b23b9fe921afbceb
m6-one-shot  enabled=True  triggers=['once']
  afe0b466-9b16-4087-8a62-a03d66754188  phase=completed  source=timer_delivery  started=2026-10-18 07:13:28+00:00
    dispatch=dispatch_d0a700bbcd694a12a321dcb6d3564e0a  response=resp_0eb75e03df3ae76f006ad4720f0c0081959d6c772b22051c6b
m6-weekday-digest  enabled=False  triggers=['weekday-morning']
  409c8678-e60b-4586-89bd-8773c85b0ccb  phase=completed  source=queued_dispatch  started=2026-10-18 07:13:31+00:00
    dispatch=dispatch_f2f45541ea064f3dad3960f0880d2503  response=resp_00c38c5b00c869e9006ad4719b9ed48190994dfcdebe9f3d46
```

The C# run shows the same three sources, each with `phase=completed` and a response id; only the names carry the `m6h3-` prefix.

The timer ran by itself: its run moved from `queued` to `completed` with a response id, and it will not fire again. A completed phase means the downstream call was accepted; the response id leads to the agent response and its trace.

## Verify

You have verified the demo when step 5 lists three routines whose runs all show `phase=completed`, with sources `timer_delivery`, `queued_dispatch` and `schedule_delivery`.

## Cleanup

`cleanup.py` deletes the three routines and the agent. In a project that was created minutes ago, deleting a schedule routine can fail with `MissingConnectorGatewayArmId` until the connector gateway finishes provisioning; run it again after a few minutes.

```bash
.venv/Scripts/python cleanup.py
dotnet run -- cleanup
```

```text
deleted routine m6-weekday-digest
deleted routine m6-one-shot
deleted routine m6-azd-digest
deleted agent m6-digest-agent
```

C# prints the same lines with the `m6h3-` names:

```text
deleted routine m6h3-weekday-digest
deleted routine m6h3-one-shot
deleted routine m6h3-azd-digest
deleted agent m6h3-digest-agent
```
