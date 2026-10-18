# Give an Agent History and Memory with Context Providers

An agent that cannot recall what you told it last week feels broken, but pasting the whole past into every prompt is slow and expensive. In this demo you attach history, a custom preference provider and a file-backed transcript to a trail-guide agent, then wire in Foundry managed memory. You finish with an agent whose recall is controlled by one list of providers.

What you practice here: shaping model input with context_providers.

Duration: about 40 minutes.

## Prerequisites

- Python 3.12 or later, the Azure CLI signed in with `az login`, and a Foundry project with `gpt-5.4` and `text-embedding-3-small` deployments.
- For the C# step: .NET 10 SDK.
- For Step 4 only: the project's managed identity needs the Foundry User role on the Foundry resource. The assignment is shown in the step.

## Run it

```powershell
cd context-providers-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME` and `EMBEDDING_MODEL_DEPLOYMENT_NAME` in `.env`. The finished projects are `context-providers-py/` and `context-providers-cs/`.

## Step 1: Control history with explicit providers

Two `InMemoryHistoryProvider` instances run side by side. The primary one replays the transcript to the model; the audit one only records, including any context other providers injected. `options={"store": False}` stops the Responses service from keeping the conversation, so the providers own it.

```python
primary = InMemoryHistoryProvider("memory", load_messages=True)
audit = InMemoryHistoryProvider("audit", load_messages=False, store_context_messages=True)

agent = Agent(
    client=build_client(),
    name="TrailGuide",
    instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
    context_providers=[primary, audit],
)

session = agent.create_session()
options = {"store": False}
await agent.run("I hike with a dog and I dislike steep climbs.", session=session, options=options)
result = await agent.run("Which kind of trail suits me?", session=session, options=options)

for source_id, scoped in session.state.items():
    print(f"{source_id}: {len(scoped['messages'])} messages")
```

```powershell
python history_providers.py
```

Expected output:

```text
A dog-friendly, low-gradient loop or out-and-back trail with smooth footing and modest elevation gain would suit you best. Look for routes labeled easy or moderate, with shade and nearby water access if your dog needs frequent breaks.
memory: 4 messages
audit: 4 messages
```

The answer reflects the dog and the dislike of steep climbs from the first call. Each provider keeps its own slice of `session.state`, keyed by its `source_id`.

## Step 2: Write a context provider that learns preferences

`PreferenceProvider` reads facts from the user's messages after each run and injects them as instructions before the next. Its data lives in the provider-scoped `state`, which is part of the session.

```python
class PreferenceProvider(ContextProvider):
    def __init__(self) -> None:
        super().__init__("preferences")

    async def before_run(self, *, agent, session, context, state) -> None:
        preferences = state.get("preferences", {})
        if preferences:
            lines = "; ".join(f"{key}: {value}" for key, value in preferences.items())
            context.extend_instructions(self.source_id, f"Known user preferences: {lines}.")

    async def after_run(self, *, agent, session, context, state) -> None:
        preferences = state.setdefault("preferences", {})
        for message in context.input_messages:
            for key, value in PATTERN.findall(message.text or ""):
                preferences[key.lower()] = value.strip()
```

`run_preferences.py` tells the agent two favorites, prints what the provider stored, serializes the session, restores it into a brand-new agent and asks for a suggestion.

```powershell
python run_preferences.py
```

Expected output:

```text
{
  "preferences": {
    "trail surface": "gravel",
    "season": "autumn"
  }
}
Try a moderate gravel loop in a forested state park this weekend, ideally 6–8 miles with a few overlooks so you can catch the autumn colors at their best. Start mid-morning for cool temps and good light, and bring layers plus water since fall weather can shift quickly.
```

The suggestion mentions gravel and autumn although the second agent never saw the first message. The facts travelled inside the serialized session.

## Step 3: Persist the transcript with a file-backed history provider

`FileHistoryProvider` writes one JSON Lines file per session ID, so history survives the process without you serializing the session. It is marked experimental in Agent Framework and prints a warning.

```python
agent = Agent(
    client=build_client(),
    name="TrailGuide",
    instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
    context_providers=[FileHistoryProvider("history")],
)

session = AgentSession(session_id=session_id)
result = await agent.run(message, session=session, options={"store": False})
```

```powershell
python run_file_history.py alex-trip "I hike with a dog and I dislike steep climbs."
python run_file_history.py alex-trip "Which kind of trail suits me?"
(Get-Content history/alex-trip.jsonl).Count
```

Expected output:

```text
ExperimentalWarning: [FILE_HISTORY] FileHistoryProvider is experimental and may change or be removed in future versions without notice.
Try Contoso’s Riverside Loop or Pine Meadow Trail ...
ExperimentalWarning: [FILE_HISTORY] FileHistoryProvider is experimental and may change or be removed in future versions without notice.
A gentle, dog-friendly loop or out-and-back trail with low elevation gain will suit you best. Look for shaded paths with good footing, water access, and posted pet rules.
4
```

Four lines, one per message, sit in `history/alex-trip.jsonl`. The session ID is the key; swapping this provider for `RedisHistoryProvider` or `CosmosHistoryProvider` changes where the lines live, not the agent code.

## Step 4: Add Foundry managed memory

History replays everything; memory keeps extracted facts. This step creates a memory store and recalls a fact from a second, separate session. It needs the project's managed identity to call your embedding deployment, which Learn documents as the Foundry User role on the resource that holds the project.

```powershell
$resourceId = az cognitiveservices account show -g <resource-group> -n <foundry-resource> --query id -o tsv
$principalId = az resource show --ids "$resourceId/projects/<project>" --query identity.principalId -o tsv
az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal --role "Foundry User" --scope $resourceId
```

```powershell
python create_memory_store.py
python run_foundry_memory.py
```

`run_foundry_memory.py` tells one session about the dog, waits ten seconds for the asynchronous extraction, then asks a different session which trail suits you and searches the store.

```python
memory = FoundryMemoryProvider(
    project_client=project,
    memory_store_name=os.environ["MEMORY_STORE_NAME"],
    scope=SCOPE,
    update_delay=0,
)
agent = Agent(
    client=client,
    name="TrailGuide",
    instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences and use remembered facts.",
    context_providers=[memory],
    default_options={"store": False},
)

first = agent.create_session()
await agent.run("I hike with a dog called Bruno, and I dislike steep climbs.", session=first)
await asyncio.sleep(10)

second = agent.create_session()
result = await agent.run("Which kind of trail suits me?", session=second)
```

This step was not run to completion while authoring. The project's managed identity had no role on the Foundry resource, and the authoring account was not allowed to assign one there; the memory store was created, and every search and write failed with a 401 from the embedding deployment.

After you assign the role, the second session's answer should mention Bruno or the dislike of steep climbs, and the search lists the extracted facts as `memory:` lines. The provider logs "Failed to search contextual memories" and the agent answers without recall while the role is missing.

## Step 5: Repeat the pattern in C#

The C# project defines the same `PreferenceProvider` as an `AIContextProvider` with typed session state, and attaches it next to the default `InMemoryChatHistoryProvider`. It uses an Azure OpenAI chat client, whose history is local by default.

```csharp
ChatClientAgent agent = azureOpenAI
    .GetChatClient(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "TrailGuide",
        ChatOptions = new() { Instructions = "You are a trail guide for Contoso Outdoor. Answer in two sentences." },
        ChatHistoryProvider = new InMemoryChatHistoryProvider(),
        AIContextProviders = [new PreferenceProvider()]
    });
```

```powershell
cd ../context-providers-cs
Copy-Item .env.example .env
dotnet run -- "My favorite trail surface is gravel. My favorite season is autumn."
dotnet run -- "Suggest a weekend hike."
```

Expected output of the second run:

```text
resumed session
Try a weekend hike on a well-maintained gravel loop through mixed forest, where autumn colors and cool temperatures make for ideal hiking conditions. At Contoso Outdoor, I'd suggest choosing a moderate 6–8 mile trail with scenic overlooks so you can enjoy a full day out without rushing.
history messages: 4
{"preferences":{"trail surface":"gravel","season":"autumn"}}
```

Two runs, two processes, one session file: the history count grew from 2 to 4 and the preferences came back from the saved session.

## Verify

Step 2 and Step 5 both recall gravel and autumn in a process that never heard them. Step 3 leaves four lines in `history/alex-trip.jsonl`. List the history file to confirm.

```powershell
Get-ChildItem history
```

Expected output:

```text
Name
----
alex-trip.jsonl
```

## Cleanup

```powershell
python delete_memory_store.py
Remove-Item -Recurse -Force history, sessions
```

Run `Remove-Item -Recurse -Force sessions` in `context-providers-cs/` too. If you assigned the role in Step 4 only for this demo, remove it with `az role assignment delete`.
