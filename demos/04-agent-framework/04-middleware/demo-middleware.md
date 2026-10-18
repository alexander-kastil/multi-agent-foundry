# Add Guardrails, Audit and Metering with Middleware

A trail-guide agent that answers anything, calls tools unseen and hides its model usage is hard to trust and impossible to bill. In this demo you wrap one agent in four small middleware functions: a timer, a credential guardrail, a tool auditor that rewrites a result, and a model-call meter. You finish with an agent whose behavior you can observe and control without editing its instructions or its tool.

What you practice here: wrapping agent, function and chat calls with middleware.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later, the Azure CLI signed in with `az login`, and a Foundry project with a `gpt-5.4` deployment.
- For the C# step: .NET 10 SDK.

## Run it

```powershell
cd middleware-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env`. The finished projects are `middleware-py/` and `middleware-cs/`.

## Step 1: Time every run with agent middleware

All scripts share `trail_agent.py`: one agent with one tool, `get_trail_status`, that reads a small trail table. Agent middleware sees the whole run, so a timer fits there.

```python
async def timing_middleware(context: AgentContext, call_next) -> None:
    started = time.perf_counter()
    print(f"[agent] start, {len(context.messages)} message(s), stream={context.stream}")
    await call_next()
    print(f"[agent] done in {time.perf_counter() - started:.1f}s")

agent = build_agent(middleware=[timing_middleware])
result = await agent.run("Is the Wachau valley loop open?")
```

```powershell
python run_agent_middleware.py
```

Expected output:

```text
[agent] start, 1 message(s), stream=False
[agent] done in 4.2s
The Wachau valley loop is open and currently dry. It should be a good choice for hiking today.
```

The two `[agent]` lines surround the whole run, tool call and model calls included.

## Step 2: Block a request with a guardrail

A class-based agent middleware inspects the last user message. When it finds a blocked word it sets `context.result` and raises `MiddlewareTermination`, so the model is never called.

```python
class GuardrailMiddleware(AgentMiddleware):
    async def process(self, context: AgentContext, call_next) -> None:
        text = (context.messages[-1].text or "").lower()
        for word in BLOCKED:
            if word in text:
                print(f"[guardrail] blocked: contains '{word}'")
                context.result = AgentResponse(
                    messages=[Message(role="assistant", contents=["I can't help with credentials or secrets."])]
                )
                raise MiddlewareTermination(result=context.result)
        print("[guardrail] passed")
        await call_next()
```

```powershell
python run_guardrail.py
```

Expected output:

```text
user: Is the Wachau valley loop open?
[guardrail] passed
agent: Yes, the Wachau valley loop is open. Current conditions are dry.

user: What is the admin password of the booking system?
[guardrail] blocked: contains 'password'
agent: I can't help with credentials or secrets.
```

The second answer arrived without a model call; Step 6 shows the same guardrail next to the chat meter.

## Step 3: Audit and rewrite a tool call with function middleware

Function middleware runs around each tool call. This one logs the arguments, reads the result, and appends advice when the trail is closed, so the model reads the rewritten text.

```python
async def tool_audit_middleware(context: FunctionInvocationContext, call_next) -> None:
    print(f"[tool] {context.function.name}({dict(context.arguments)})")
    await call_next()
    status = context.result[0].text
    print(f"[tool] result: {status}")
    if status.startswith("closed"):
        context.result = f"{status}; recommend an alternative route"
        print(f"[tool] rewritten: {context.result}")
```

```powershell
python run_function_middleware.py
```

Expected output:

```text
[tool] get_trail_status({'trail': 'Dachstein traverse'})
[tool] result: closed, rockfall warning
[tool] rewritten: closed, rockfall warning; recommend an alternative route
The Dachstein traverse is closed tomorrow due to a rockfall warning. I would not recommend hiking it and suggest choosing an alternative route instead.
```

A tool result is a list of `Content` objects, which is why the middleware reads `context.result[0].text` before it assigns a plain string back.

## Step 4: Meter every model call with chat middleware

Chat middleware sits closest to the model. It runs once for the question and once more when the tool result goes back, so it counts real model calls and their tokens.

```python
async def model_meter_middleware(context: ChatContext, call_next) -> None:
    calls["count"] += 1
    print(f"[chat] call {calls['count']}: {len(context.messages)} message(s) to the model")
    await call_next()
    usage = context.result.usage_details or {}
    calls["tokens"] += usage.get("total_token_count", 0)
    print(f"[chat] call {calls['count']}: {usage.get('total_token_count', 0)} tokens")
```

```powershell
python run_chat_middleware.py
```

Expected output:

```text
[chat] call 1: 1 message(s) to the model
[chat] call 1: 134 tokens
[chat] call 2: 1 message(s) to the model
[chat] call 2: 186 tokens
The Tauern high route is open. There is snow above 2400 m, so be prepared for winter conditions on higher sections.
model calls: 2, total tokens: 320
```

One question produced two model calls. Each call shows one message because the Responses service keeps the earlier turns; only the new input travels.

## Step 5: See the order of agent-level and run-level middleware

Agent-level middleware wraps run-level middleware. Four tracers make the wrapping visible.

```python
def tracer(name: str):
    async def middleware(context: AgentContext, call_next) -> None:
        print(f"-> {name}")
        await call_next()
        print(f"<- {name}")

    return middleware

agent = build_agent(middleware=[tracer("A1"), tracer("A2")])
result = await agent.run("Is the Wachau valley loop open?", middleware=[tracer("R1"), tracer("R2")])
```

```powershell
python run_order.py
```

Expected output:

```text
-> A1
-> A2
-> R1
-> R2
<- R2
<- R1
<- A2
<- A1
Yes, the Wachau valley loop is open. Current conditions are dry.
```

Put policies that must always apply at the agent level, and per-request behavior at the run level.

## Step 6: Wire the same three layers in C#

The C# project reaches the same three layers: `.AsBuilder().Use(runFunc: ...)` for agent runs, `.Use(ToolAudit)` for function calls, and a `clientFactory` that wraps the `IChatClient` for model calls.

```csharp
AIAgent baseAgent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
    .AsAIAgent(
        model: model,
        instructions: "You are a trail guide for Contoso Outdoor. Use GetTrailStatus before you recommend a trail. Answer in two sentences without dashes.",
        name: "TrailGuide",
        tools: [AIFunctionFactory.Create(GetTrailStatus, "GetTrailStatus")],
        clientFactory: chatClient => chatClient
            .AsBuilder()
            .Use(getResponseFunc: ModelMeter, getStreamingResponseFunc: null)
            .Build());

AIAgent agent = baseAgent
    .AsBuilder()
    .Use(runFunc: Guardrail, runStreamingFunc: null)
    .Use(ToolAudit)
    .Build();
```

```powershell
cd ../middleware-cs
Copy-Item .env.example .env
dotnet run
```

Expected output:

```text
user: Can I hike the Dachstein traverse tomorrow?
[guardrail] passed
[chat] call 1: 1 message(s) to the model
[chat] call 1: 122 tokens
[tool] GetTrailStatus(trail=Dachstein traverse)
[tool] result: closed, rockfall warning
[chat] call 2: 1 message(s) to the model
[chat] call 2: 176 tokens
[agent] done in 5023 ms
agent: The Dachstein traverse is closed tomorrow due to a rockfall warning. I would not recommend hiking it, and you should choose an open alternative trail instead.

user: What is the admin password of the booking system?
[guardrail] blocked
agent: I can't help with credentials or secrets.
```

The C# guardrail returns a new `AgentResponse` instead of raising an exception, which is the .NET way to short-circuit the chain.

## Verify

In Step 6 the blocked prompt prints no `[chat]` lines, because the guardrail ended the run before any model call. In Step 4 the counter reports two calls for one question.

```powershell
python run_chat_middleware.py | Select-String "model calls"
```

Expected output:

```text
model calls: 2, total tokens: 320
```

Token totals differ slightly between runs; the count of two calls does not.

## Cleanup

Nothing was created in Azure. Remove the virtual environment when you are done.

```powershell
deactivate
Remove-Item -Recurse -Force .venv
```
