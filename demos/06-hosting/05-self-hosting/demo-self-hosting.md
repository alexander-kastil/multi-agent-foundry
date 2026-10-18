# Host an Agent Behind a Responses Endpoint

A support-desk agent that only runs inside a notebook cannot serve anyone. In this demo you put the same agent behind an HTTP endpoint that any OpenAI SDK can call, keep a conversation going across turns, and decide in your own code which request options the agent accepts.

## Prerequisites

- Python 3.12 or later, .NET 10 SDK, and `az login` on the subscription that holds the class Foundry project.
- A Foundry project with a chat deployment, for example `gpt-5.4`.
- Concepts: [Self-Hosting: ASP.NET Core & Responses](readme.md). The hosting packages are alpha in Python and prerelease in .NET; the project pins exact versions.

## Run it

```bash
cd self-hosting-py
python -m venv .venv
.venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
hypercorn app:app --bind 127.0.0.1:8000
```

Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first. The server blocks, so run the steps below in a second terminal with the same virtual environment.

## Step 1: Give the agent a tool and a state holder

`app.py` defines one tool that reads a ticket table, builds the agent on a Foundry chat client, and wraps the agent factory in `AgentState`. The state object builds the agent once and keeps sessions by id.

```python
@tool
def lookup_ticket(ticket_id: str) -> str:
    """Return the status line for a support ticket id such as T-1001."""
    return TICKETS.get(ticket_id.upper(), f"No ticket {ticket_id} found")


def create_agent() -> Agent:
    return Agent(
        client=FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=DefaultAzureCredential(),
        ),
        name="support-desk",
        instructions="You are the Contoso support desk agent. Use lookup_ticket for ticket questions and answer in two sentences.",
        tools=[lookup_ticket],
        default_options={"store": False},
    )


state = AgentState(create_agent)
```

Start the server and watch the log for the listening line. `default_options={"store": False}` keeps the model service from storing the conversation, because this host keeps the session itself.

Expected output:

```text
[2026-10-18 09:03:05 +0200] [63452] [INFO] Running on http://127.0.0.1:8000 (CTRL + C to quit)
```

## Step 2: Own the Responses route

The route is ordinary FastAPI code. The helpers convert the request and the result; the allowlist line is your policy.

```python
ALLOWED_REQUEST_OPTIONS = frozenset({"max_tokens"})


@app.post("/responses", response_model=None)
async def responses(body: dict[str, Any] = Body(...)) -> JSONResponse:
    try:
        run = responses_to_run(body)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    session_id, is_conversation_id = responses_session_id(body)
    conversation_id = session_id if is_conversation_id else None
    response_id = create_response_id()
    options = {key: value for key, value in run["options"].items() if key in ALLOWED_REQUEST_OPTIONS}

    target = await state.get_target()
    session = await state.get_or_create_session(session_id or response_id)
    result = await target.run(run["messages"], session=session, options=options)
    await state.set_session(conversation_id or response_id, session)

    return JSONResponse(responses_from_run(result, response_id=response_id, conversation_id=conversation_id))
```

Send a raw request with an option the allowlist does not contain.

```bash
curl -s -X POST http://localhost:8000/responses -H "Content-Type: application/json" -d '{"input":"Status of T-1003?","temperature":0.1}'
```

Expected output (trimmed):

```text
{"id":"resp_ae70f3c682ac4f3a9890abf2a06d997c","object":"response","model":"gpt-5.4-2026-03-05","output":[{"arguments":"{\"ticket_id\":\"T-1003\"}","name":"lookup_ticket","type":"function_call", ...},{"output":[{"text":"New starter laptop request, assigned to procurement, priority medium","type":"input_text"}], ...}, ...
```

The output shows the tool call and its result next to the final message, and the `temperature` value never reached the model call.

![One request through the Python Responses route: responses_to_run converts the body, your allowlist keeps only max_tokens, get_or_create_session loads the session from the SessionStore, target.run runs the support-desk agent with lookup_ticket against the Foundry model, set_session saves it and responses_from_run builds the reply](../pptx/images/05-01-responses-route.svg)

## Step 3: Call it with the OpenAI SDK

`call_server.py` points the standard OpenAI client at your server. The second call passes `previous_response_id`, so the server loads the session saved after the first answer.

```python
client = OpenAI(base_url=os.getenv("SERVER_URL", "http://localhost:8000"), api_key="not-needed")

first = client.responses.create(model="support-desk", input=sys.argv[1])
print(first.output_text)

second = client.responses.create(model="support-desk", input=sys.argv[2], previous_response_id=first.id)
print(second.output_text)
```

```bash
python call_server.py "What is the status of ticket T-1002?" "And who is it assigned to?"
```

Expected output (the wording varies per run):

```text
Ticket T-1002 is: "VPN drops every hour for the sales team," and it is currently assigned to the network team. Its priority is high.
It is assigned to the network team. The ticket concerns VPN drops every hour for the sales team.
```

The second answer uses "it" without naming the ticket, which only works because the session came back with the first turn. An id the server never saw starts a fresh session instead of failing.

```bash
curl -s -X POST http://localhost:8000/responses -H "Content-Type: application/json" -d '{"input":"What did I just ask?","previous_response_id":"resp_doesnotexist"}'
```

Expected output (trimmed):

```text
"You asked, \"What did I just ask?\"  That was your most recent message."
```

That is why the readme tells you to authenticate and authorize every continuation id before you use it. Stop the server with Ctrl+C.

## Step 4: Host the same agent in ASP.NET Core

The C# project replaces the route code with two calls. `AddAIAgent` registers the agent, and `MapOpenAIResponses` and `MapOpenAIConversations` expose the Responses and Conversations endpoints.

```csharp
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

var supportDesk = builder.AddAIAgent(
    "support-desk",
    (services, name) => services.GetRequiredService<IChatClient>().AsAIAgent(
        instructions: "You are the Contoso support desk agent. Use lookup_ticket for ticket questions and answer in two sentences.",
        name: name,
        tools: [lookupTicket]));

var app = builder.Build();

app.MapOpenAIResponses(supportDesk);
app.MapOpenAIConversations();
```

```bash
cd self-hosting-cs
PROJECT_ENDPOINT=<endpoint> MODEL_DEPLOYMENT_NAME=gpt-5.4 ASPNETCORE_URLS=http://localhost:8001 dotnet run
```

The C# project ships no request-option allowlist; authentication, authorization and request validation stay in your ASP.NET Core middleware pipeline.

## Step 5: Continue a conversation by id

A conversation id stays stable across turns, unlike a response id. Create one, then send both turns against it.

![Two ways to continue: in Step 3 previous_response_id gives every turn a new id and session key; in Step 5 a conversation id created by POST /v1/conversations stays the same for both turns and one session](../pptx/images/05-02-conversation-ids.svg)

```bash
curl -s -X POST http://localhost:8001/v1/conversations -H "Content-Type: application/json" -d '{}'
```

Expected output:

```text
{"id":"conv_2PZJshEKclbgFa1GqjBby0JXrXnX1CLashdxrP0kOqvuVCSX","object":"conversation","created_at":1792307066,"metadata":{}}
```

```bash
curl -s -X POST http://localhost:8001/support-desk/v1/responses -H "Content-Type: application/json" -d '{"conversation":"<conversation id>","input":"What is the status of ticket T-1002?"}'
curl -s -X POST http://localhost:8001/support-desk/v1/responses -H "Content-Type: application/json" -d '{"conversation":"<conversation id>","input":"And who is it assigned to?"}'
```

Expected output, first and second response (extracted from the JSON):

```text
output types: ['function_call', 'function_call_output', 'message']
Ticket T-1002: VPN drops every hour for the sales team, assigned to network, priority high. Let me know if you want me to check another ticket.

output types: ['message']
It is assigned to the network team. Let me know if you want the full ticket status again.
```

The first turn calls the tool; the second answers from the conversation without calling it again. Stop the server with Ctrl+C.

## Verify

Both hosts answer a follow-up that depends on the first turn: the Python host through `previous_response_id`, the C# host through a `conversation` id. In Step 2 the response carries the `lookup_ticket` call, which proves the tool ran inside your process.

## Cleanup

Nothing was created in Azure. Stop both servers with Ctrl+C and delete the `.venv` folder if you no longer need it.
