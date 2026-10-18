# Resume a Conversation After the Process Exits

A chat that forgets everything when the script ends cannot power a support desk or a planner that users return to tomorrow. In this demo you prove what a session does, save it to disk, resume it from a separate process, rebuild it from a service ID alone, and keep one conversation per user. You finish with a small session store that works for any agent.

What you practice here: persisting an AgentSession with to_dict and from_dict.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later, the Azure CLI signed in with `az login`, and a Foundry project with a `gpt-5.4` deployment.
- For the C# step: .NET 10 SDK.

## Run it

```powershell
cd agent-session-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env`. The finished projects are `agent-session-py/` and `agent-session-cs/`.

## Step 1: See what a session adds

The same agent answers the same question twice. The first call reuses the session that heard the introduction; the second has no session.

```python
session = agent.create_session()
await agent.run("My name is Alex and I plan a kayak trip on the Danube.", session=session)
with_session = await agent.run("Which river did I mention?", session=session)

without_session = await agent.run("Which river did I mention?")
```

```powershell
python session_basics.py
```

Expected output:

```text
same session : You mentioned the Danube. Alex, I can also help you plan your kayak route there.
no session   : You haven't mentioned a river in this conversation. If you tell me the river, I can help with planning around it.
```

## Step 2: Save the session to a file and resume it in a new process

Each run of `session_file.py` is a separate Python process. It loads `sessions/session.json` when the file exists, otherwise it creates a session, and it writes the session back after the answer.

```python
if SESSION_FILE.exists():
    session = AgentSession.from_dict(json.loads(SESSION_FILE.read_text()))
else:
    session = agent.create_session()

result = await agent.run(message, session=session)

SESSION_FILE.write_text(json.dumps(session.to_dict(), indent=2))
```

```powershell
python session_file.py "My name is Alex and I plan a kayak trip on the Danube."
python session_file.py "Which river did I mention?"
Get-Content sessions/session.json
```

Expected output:

```text
new session 0b299817-282e-46aa-81f0-dfad7b2fa24d
Alex, for your kayak trip on the Danube, I'd suggest planning around calmer morning conditions, checking local river sections and border rules in advance, and packing a PFD, dry bags, sun protection, and a simple repair kit.
resumed session 0b299817-282e-46aa-81f0-dfad7b2fa24d
You mentioned the Danube. Alex, if you want, I can help you plan your kayak trip there.
{
  "type": "session",
  "session_id": "0b299817-282e-46aa-81f0-dfad7b2fa24d",
  "service_session_id": "resp_030544eaa0e2f0e0006ad46001ed7481939b13f596ba0f6f6b",
  "state": {}
}
```

The file holds no messages. `FoundryChatClient` stores the conversation in Foundry, so the session only carries the `resp_` ID that points to it.

## Step 3: Rebuild a session from the service ID alone

A different worker, with no access to `session.json` beyond the ID, can still continue the conversation. `get_session(service_session_id=)` wraps the ID in a fresh `AgentSession`.

```python
saved = json.loads(Path("sessions/session.json").read_text())
agent = build_agent()
session = agent.get_session(service_session_id=saved["service_session_id"])
result = await agent.run("Summarize what you know about my trip in one sentence.", session=session)
```

```powershell
python session_service_id.py
```

Expected output:

```text
service_session_id: resp_030544eaa0e2f0e0006ad46001ed7481939b13f596ba0f6f6b
Alex, you're planning a kayak trip on the Danube.
```

## Step 4: Keep the history inside the session

Some deployments cannot or must not keep conversations on the service. Passing `options={"store": False}` makes Agent Framework keep the transcript locally, in the session state, through `InMemoryHistoryProvider`.

```python
OPTIONS = {"store": False}

result = await agent.run(message, session=session, options=OPTIONS)

data = session.to_dict()
print(f"service_session_id: {data['service_session_id']}")
print(f"stored messages   : {len(data['state']['in_memory']['messages'])}")
```

```powershell
python session_local_history.py "My name is Alex and I plan a kayak trip on the Danube."
python session_local_history.py "Which river did I mention?"
```

Expected output:

```text
Nice to meet you, Alex. The Danube can be a great kayak trip with a mix of calm stretches and busier sections depending on where you launch. ...
service_session_id: None
stored messages   : 2
You mentioned the Danube. You said you were planning a kayak trip on it, Alex.
service_session_id: None
stored messages   : 4
```

Open `sessions/local-session.json` and you find all four messages. The file now grows with the conversation, and that growth is what history providers and compaction manage later in this module.

## Step 5: Give every user their own session

A single file is fine for one user. A real app maps its own user ID to a session and never accepts a service ID from a client. `session_registry.py` stores one serialized session per user in `sessions/registry.json`.

```python
registry = load_registry()

if user in registry:
    session = AgentSession.from_dict(registry[user])
else:
    session = agent.create_session()

result = await agent.run(message, session=session)

registry[user] = session.to_dict()
```

```powershell
python session_registry.py alice "I am Alice and I want to hike the Tauern."
python session_registry.py bob "I am Bob and I want to cycle the Danube."
python session_registry.py alice "What did I want to do?"
python session_registry.py bob "What did I want to do?"
```

Expected output:

```text
alice: Alice, the Tauern are a great choice for hiking, with scenic alpine trails and options ranging from hut-to-hut treks to high-mountain routes. ...
bob: Sounds great, Bob. Cycling the Danube is a classic trip with scenic riverside paths, charming towns, and generally gentle riding. ...
alice: You wanted to hike the Tauern, Alice. If you'd like, I can help plan the trip.
bob: You said you're Bob and you want to cycle the Danube. I can also plan that ride if you want.
```

Each user got an answer from their own conversation. The registry here is a file for clarity; in production it is a database row, and the lookup is where you check the authenticated user.

## Step 6: Do the same in C#

The C# project runs the same flow with `CreateSessionAsync`, `SerializeSessionAsync` and `DeserializeSessionAsync`, then rebuilds a second session from the conversation ID.

```csharp
JsonElement serialized = await agent.SerializeSessionAsync(session);
await File.WriteAllTextAsync(sessionFile, JsonSerializer.Serialize(serialized, new JsonSerializerOptions { WriteIndented = true }));

var conversationId = serialized.GetProperty("conversationId").GetString()!;
AgentSession byServiceId = await agent.CreateSessionAsync(conversationId);
```

```powershell
cd ../agent-session-cs
Copy-Item .env.example .env
dotnet run -- "My name is Alex and I plan a kayak trip on the Danube."
dotnet run -- "Which river did I mention?"
```

Expected output of the second run:

```text
resumed session
You mentioned the Danube. Alex, that's the river you said you plan to kayak.
{
  "conversationId": "resp_03123d85414e0019006ad46033c1208196bc5f4a583fc39b40",
  "stateBag": {
    "InMemoryChatHistoryProvider": {
      "messages": []
    }
  }
}
Alex, you're planning a kayak trip on the Danube.
```

The C# JSON uses `conversationId` where Python uses `service_session_id`, and the two formats are not interchangeable.

## Verify

The first answer of the second process in Step 2 names the Danube although that process never saw the introduction. In Step 4 the stored message count climbs from 2 to 4.

```powershell
python -c "import json; print(json.load(open('sessions/local-session.json'))['state']['in_memory']['messages'][0]['contents'][0]['text'])"
```

Expected output:

```text
My name is Alex and I plan a kayak trip on the Danube.
```

## Cleanup

```powershell
Remove-Item -Recurse -Force sessions
```

Run the same command in `agent-session-cs/`. The conversations created in Foundry carry no billable resources.
