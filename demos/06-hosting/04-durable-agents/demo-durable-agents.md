# Run an Approval Workflow That Survives a Restart

A release announcement needs a human to approve it before it goes out, and that human may answer in an hour or in three days. Held in a normal process, that wait pins a server, and a restart loses the draft, the conversation and the reviewer's feedback. You will build a writer agent whose approval loop lives in Azure Functions and the Durable Task Scheduler, kill the host while it waits, and finish the approval afterwards.

What you build: an Azure Functions app with a durable `AnnouncementWriter` agent, a three-round approval orchestration with a 72 hour timeout, and the same app deployed to Flex Consumption. Duration: about 45 minutes.

## Prerequisites

- Azure CLI signed in (`az login`), Azure Functions Core Tools 4.x, Docker, and a Microsoft Foundry project with a `gpt-5.4` deployment.
- Python 3.12 for the Python version, or the .NET 10 SDK for the C# version.
- Permission to create resources and role assignments in the resource group you deploy to in Step 5.
- The reviewer in this demo is you, calling `curl`. No messaging system is involved.

## Run it

Start the two local dependencies. Azurite backs the Functions runtime, the Durable Task Scheduler emulator backs the durable state.

```bash
docker run -d --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
docker run -d --name dts-emulator -p 8080:8080 -p 8082:8082 mcr.microsoft.com/dts/dts-emulator:latest
```

Python:

```bash
cd durable-agents-py
python -m venv .venv
source .venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
cp local.settings.sample.json local.settings.json
func start
```

C#:

```bash
cd durable-agents-cs
cp local.settings.sample.json local.settings.json
func start
```

Put your project endpoint into `.env` (Python) or into `PROJECT_ENDPOINT` in `local.settings.json` (C#) first. Leave `func start` running and use a second terminal for the `curl` calls below.

## Step 1: Host the writer agent

Register the agent with the Durable extension. This single call generates the HTTP endpoint and moves every conversation into a durable entity.

```python
writer = Agent(
    client=FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    ),
    name="AnnouncementWriter",
    instructions="You write short product release announcements. Return JSON with title and body.",
)

app = AgentFunctionApp(agents=[writer])
```

```csharp
AIAgent writer = new AIProjectClient(endpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You write short product release announcements. Return JSON with title and body.",
        name: "AnnouncementWriter");

using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options => options.AddAIAgent(writer))
    .Build();
```

Expected output (Python host, trimmed):

```text
Functions:

	http-AnnouncementWriter: [POST] http://localhost:7071/api/agents/AnnouncementWriter/run

	dafx-AnnouncementWriter: entityTrigger
```

The C# host lists the same two entries plus the approval functions you add in Step 2.

Call the agent, then continue the conversation by passing the returned session id:

```bash
curl -i -X POST http://localhost:7071/api/agents/AnnouncementWriter/run -H "Content-Type: text/plain" -d "Write one sentence welcoming users to Contoso Portal 2.4."
```

Expected output:

```text
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
x-ms-session-id: 0a204ff31da0423a848e21c8725b152f

{"title":"Welcome to Contoso Portal 2.4","body":"Welcome to Contoso Portal 2.4, bringing you an improved experience with the latest updates and enhancements."}
```

```bash
curl -X POST "http://localhost:7071/api/agents/AnnouncementWriter/run?session_id=0a204ff31da0423a848e21c8725b152f" -H "Content-Type: text/plain" -d "Shorten it to five words."
```

Expected output:

```text
{"title":"Welcome to Contoso Portal 2.4","body":"Welcome to Contoso Portal 2.4!"}
```

The second call shortened the first answer, so the history lives in the session and not in your process.

## Step 2: Draft with structured output

Add an orchestration that asks the agent for a typed draft. The response format makes the draft an `Announcement` object instead of free text, so later steps can pass it to activities.

```python
class Announcement(BaseModel):
    title: str
    body: str


@app.orchestration_trigger(context_name="context")
def announcement_approval(context: DurableOrchestrationContext):
    topic = context.get_input()["topic"]
    agent = app.get_agent(context, WRITER)
    session = agent.create_session()

    draft = yield agent.run(
        messages=f"Write a release announcement about {topic}.",
        session=session,
        options={"response_format": Announcement},
    )
    announcement = draft.value
```

```csharp
public record Announcement(string Title, string Body);

[Function(nameof(RunAnnouncementApproval))]
public static async Task<Announcement> RunAnnouncementApproval([OrchestrationTrigger] TaskOrchestrationContext context)
{
    var topic = context.GetInput<StartRequest>()!.Topic;
    DurableAIAgent writer = context.GetAgent(Writer);
    AgentSession session = await writer.CreateSessionAsync();

    var draft = await writer.RunAsync<Announcement>($"Write a release announcement about {topic}.", session);
    var announcement = draft.Result;
}
```

Start an instance through the `start` function:

```bash
curl -X POST http://localhost:7071/api/approvals/run -H "Content-Type: application/json" -d '{"topic":"Contoso Portal 2.4"}'
```

Expected output:

```text
{"instanceId": "1a550c1248e54b8aad545386098a4c47"}
```

Keep the instance id in a shell variable for the next steps:

```bash
INSTANCE=1a550c1248e54b8aad545386098a4c47
```

## Step 3: Pause for a human decision

Notify the reviewer with an activity, then wait for the `ApprovalDecision` event or a 72 hour timer, whichever comes first. The orchestration is suspended while it waits.

```python
for attempt in range(3):
    context.set_custom_status(f"waiting for review, round {attempt + 1}")
    yield context.call_activity("request_review", announcement.model_dump())

    approval = context.wait_for_external_event(APPROVAL_EVENT)
    timeout = context.create_timer(context.current_utc_datetime + timedelta(hours=72))
    winner = yield context.task_any([approval, timeout])
    if winner != approval:
        raise TimeoutError("No review decision within 72 hours")
    timeout.cancel()
```

```csharp
for (var attempt = 1; attempt <= 3; attempt++)
{
    context.SetCustomStatus($"waiting for review, round {attempt}");
    await context.CallActivityAsync(nameof(RequestReview), announcement);

    Decision decision;
    try
    {
        decision = await context.WaitForExternalEvent<Decision>(ApprovalEvent, TimeSpan.FromHours(72));
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException("No review decision within 72 hours");
    }
}
```

Poll the `status` function while the model drafts. It takes a few seconds to reach the wait:

```bash
curl http://localhost:7071/api/approvals/status/$INSTANCE
```

Expected output:

```text
{"runtimeStatus": "Running", "workflowStatus": "waiting for review, round 1", "output": null}
```

The host log shows the activity that stands in for the notification:

```text
REVIEW REQUESTED: Contoso Portal 2.4 is here
```

## Step 4: Reject, restart the host, approve

The `decide` function raises the event on the instance. Send a rejection with feedback first.

```bash
curl -X POST http://localhost:7071/api/approvals/decision/$INSTANCE -H "Content-Type: application/json" -d '{"approved":false,"feedback":"Mention the new audit log and keep it under 40 words."}'
```

Expected output:

```text
{"sent": false}
```

The orchestration reuses the writer session so the agent knows its first draft, rewrites it, and asks for review again:

```python
decision = Decision.model_validate_json(approval.result)
if decision.approved:
    yield context.call_activity("publish", announcement.model_dump())
    context.set_custom_status("published")
    return announcement.model_dump()

revised = yield agent.run(
    messages=f"The reviewer rejected it. Rewrite it with this feedback: {decision.feedback}",
    session=session,
    options={"response_format": Announcement},
)
announcement = revised.value
```

```csharp
if (decision.Approved)
{
    await context.CallActivityAsync(nameof(Publish), announcement);
    context.SetCustomStatus("published");
    return announcement;
}

var revised = await writer.RunAsync<Announcement>(
    $"The reviewer rejected it. Rewrite it with this feedback: {decision.Feedback}", session);
announcement = revised.Result;
```

```bash
curl http://localhost:7071/api/approvals/status/$INSTANCE
```

Expected output:

```text
{"runtimeStatus": "Running", "workflowStatus": "waiting for review, round 2", "output": null}
```

Now stop the host. In the `func start` terminal press `Ctrl+C`, then start it again with `func start`. The instance is waiting in the scheduler, not in the process:

```bash
curl http://localhost:7071/api/approvals/status/$INSTANCE
```

Expected output:

```text
{"runtimeStatus": "Running", "workflowStatus": "waiting for review, round 2", "output": null}
```

Approve it:

```bash
curl -X POST http://localhost:7071/api/approvals/decision/$INSTANCE -H "Content-Type: application/json" -d '{"approved":true}'
curl http://localhost:7071/api/approvals/status/$INSTANCE
```

Expected output (Python, trimmed):

```text
{"sent": true}
{"runtimeStatus": "Completed", "workflowStatus": "published", "output": {"title": "Contoso Portal 2.4 released", "body": "Contoso Portal 2.4 adds a new audit log for better visibility, along with faster performance, smoother navigation, and stability improvements. Update today to get a more reliable, efficient portal experience."}}
```

The host log ends with `PUBLISHED: Contoso Portal 2.4 released`. The C# version returns the same fields with capitalised names (`Title`, `Body`).

The model writes different text each run, so your wording will differ. The restart behaviour will not.

## Step 5: Deploy to Flex Consumption

Create the storage account, the Durable Task Scheduler, a task hub, and a Flex Consumption function app with a managed identity. The script also grants that identity the Durable Task Data Contributor role on the scheduler and the Foundry User role on your project, and writes the app settings.

From this topic folder, open `provision.azcli` and set `SUFFIX` to a value of your own and `LANGUAGE` to `py` or `cs`. The script reads `PROJECT_ENDPOINT` from `durable-agents-py/.env`. Run it, then publish the app:

```bash
bash provision.azcli
cd durable-agents-py
func azure functionapp publish m6h3-durable-agents-7k2 --python
```

For C#, publish `durable-agents-cs` to `m6h3-durable-agents-cs-7k2` with the same command and no `--python` flag.

Expected output (trimmed):

```text
The deployment was successful!
Functions in m6h3-durable-agents-7k2:
    announcement_approval - [orchestrationTrigger]
    dafx-AnnouncementWriter - [entityTrigger]
    http-AnnouncementWriter - [httpTrigger]
        Invoke url: https://m6h3-durable-agents-7k2.azurewebsites.net/api/agents/announcementwriter/run
    start - [httpTrigger]
        Invoke url: https://m6h3-durable-agents-7k2.azurewebsites.net/api/approvals/run
```

Deployed functions require a function key. Fetch it and run the same flow against Azure:

```bash
APP=m6h3-durable-agents-7k2
KEY=$(az functionapp keys list -g rg-pro-code-agents -n $APP --query functionKeys.default -o tsv)
curl -X POST "https://$APP.azurewebsites.net/api/approvals/run?code=$KEY" -H "Content-Type: application/json" -d '{"topic":"Contoso Portal 2.4"}'
```

Expected output:

```text
{"instanceId": "614bba93273c4bdabc94418475cce4ea"}
```

```bash
INSTANCE=614bba93273c4bdabc94418475cce4ea
curl "https://$APP.azurewebsites.net/api/approvals/status/$INSTANCE?code=$KEY"
curl -X POST "https://$APP.azurewebsites.net/api/approvals/decision/$INSTANCE?code=$KEY" -H "Content-Type: application/json" -d '{"approved":true}'
curl "https://$APP.azurewebsites.net/api/approvals/status/$INSTANCE?code=$KEY"
```

Expected output (trimmed):

```text
{"runtimeStatus": "Running", "workflowStatus": "waiting for review, round 1", "output": null}
{"sent": true}
{"runtimeStatus": "Completed", "workflowStatus": "published", "output": {"title": "Contoso Portal 2.4 is here", "body": "Contoso Portal 2.4 is now available, ..."}}
```

Between the first and the second call nothing is executing in Azure. The only thing alive is the orchestration record in the scheduler.

## Verify

- Step 1: a second call with `session_id` answered using the first answer.
- Step 4: the status endpoint returned `waiting for review, round 2` after you restarted the host.
- Step 5: the deployed instance reached `Completed` with `workflowStatus` `published`.
- Optional: open `http://localhost:8082` to see the instance, its timers and the `AnnouncementWriter` conversation in the emulator dashboard.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Agent and `start` calls return 500 with `Could not load file or assembly 'Microsoft.AspNetCore.Mvc.WebApiCompatShim'` | Core Tools 4.5 ships an older host than the newest extension bundle expects | Keep the pinned bundle in the Python `host.json`: `"version": "[4.31.0, 4.31.0]"` |
| C# orchestration stays `Running` after the agent answered | A newer `Microsoft.Agents.AI` than the one the hosting package was built against | Keep the pinned package set in `durable-agents-cs.csproj` |
| `ManagedIdentityCredential authentication failed ... 169.254.169.254` locally | `DefaultAzureCredential` stops at the managed identity probe on a machine with no metadata service | Add `"AZURE_TOKEN_CREDENTIALS": "dev"` to `local.settings.json` |
| `Task hub names specified in host.json and connection string must match` | `TASKHUB_NAME` differs from `TaskHub=` in the connection string | Use the same name in both |
| Two apps on one task hub, instances fail immediately | A different app picked up work it has no function for | Give each app its own task hub, as `provision.azcli` does |

## Cleanup

Stop the host and the local containers:

```bash
docker rm -f azurite dts-emulator
```

Delete what you created in Azure. This removes both function apps, the scheduler with its task hubs, and the storage account:

```bash
bash deprovision.azcli
```
