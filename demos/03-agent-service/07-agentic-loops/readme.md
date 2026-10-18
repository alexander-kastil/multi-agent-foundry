# Stateful Agentic Loops: Plan & Resume

A single model call answers a question. An agent that must research, check its own work and survive a restart needs loops: a loop that runs tools, a loop that plans and critiques, and a way to pick the work up again tomorrow. Foundry Agent Service keeps the state on the server, so each loop only has to carry ids.

This topic builds four loop designs around one prompt agent: plan then act, act then reflect, fork into parallel scenarios and persist then resume. All four rest on the same primitives from the first topic: agent versions, conversations and responses.

## The inner loop: run tools until the model answers

A prompt agent can call a function tool, but your code runs the function. The service returns a `function_call` item, your code executes it and sends back a `function_call_output`, and the loop ends when a response contains no more function calls.

```python
from azure.ai.projects.models import FunctionTool

MARKET_DATA_TOOL = FunctionTool(
    name="get_market_data",
    description="Get price, valuation and fundamentals for a stock ticker.",
    parameters={
        "type": "object",
        "properties": {"ticker": {"type": "string"}},
        "required": ["ticker"],
        "additionalProperties": False,
    },
    strict=True,
)
```

```python
response = openai.responses.create(input=user_input, extra_body=AGENT_REF)
while True:
    outputs = [
        {"type": "function_call_output", "call_id": item.call_id, "output": get_market_data(**json.loads(item.arguments))}
        for item in response.output
        if item.type == "function_call"
    ]
    if not outputs:
        break
    response = openai.responses.create(input=outputs, previous_response_id=response.id, extra_body=AGENT_REF)
```

Every outer loop in this topic calls this one function, which is why the rest of the code stays short.

## Two ways to carry state

State travels as an id, and there are two kinds.

| Mechanism | Parameter | Best for |
| --- | --- | --- |
| Response chain | `previous_response_id` | Branching: one finished state can start several independent futures |
| Conversation | `conversation` | Resuming: a durable history that survives your process |

A response chain is cheap and can fork, but it is a single line of ancestry. A conversation is a named container that you can hand to a later process, as the persist and resume pattern below does.

## Plan then act

Planning separates decomposition from execution. The first turn asks only for a plan as JSON, your code can inspect or edit the plan, and a second turn chained by `previous_response_id` executes it.

```python
plan_response = run_turn(openai, 'Decompose this task into research steps. Reply as JSON: {"steps": [...]}. Task: ...')
plan = json.loads(plan_response.output_text)
result = run_turn(openai, f"Execute this plan: {json.dumps(plan)}", previous_response_id=plan_response.id)
```

Asking for raw JSON in the agent instructions keeps `json.loads` reliable. The gate between the two turns is where you add validation, human approval or cost limits.

## Act then reflect

Reflection asks the agent to critique and revise its own answer and to score its confidence. A loop needs a stopping rule, because the model's self-score does not have to improve.

```python
for cycle in range(1, MAX_REFLECTION_CYCLES + 1):
    response = run_turn(openai, "Critique your analysis and revise it. Reply as JSON with confidence 1-10.", previous_response_id=response.id)
    trace = json.loads(response.output_text)
    if trace["confidence"] >= CONFIDENCE_THRESHOLD:
        break
```

Use two stopping conditions together: a confidence threshold and a cycle cap. In the recorded run the confidence fell on every cycle and the cap, not the threshold, ended the loop.

## Fork into parallel scenarios

Because a response id names a finished state, several prompts can branch from the same id and run side by side. Background mode makes each branch a job that you poll.

```python
job = openai.responses.create(
    input=prompt,
    previous_response_id=base_response.id,
    background=True,
    extra_body=AGENT_REF,
)
while job.status in ("queued", "in_progress"):
    time.sleep(2)
    job = openai.responses.retrieve(job.id)
```

The branches do not see each other, and none of them repeats the tool calls that produced the base state. Background mode changes how you receive a response, not what the response is.

## Persist and resume

Only ids need to be stored to resume: the history lives in Foundry, and the application keeps the pointer and its own metadata. A small store maps a session name to the conversation id.

```python
save_session("analyst-session-1", conversation.id, first.id, {"ticker": "GOOG", "agent_version": agent.version})
```

A different process loads the entry and continues the same conversation. A follow-up that never names the ticker proves the context came from the server.

```python
saved = load_session("analyst-session-1")
resumed = run_turn(openai, "Which ticker did we just discuss, and what was its P/E ratio?", conversation_id=saved["conversation_id"])
```

Store the agent version next to the conversation id when behavior must stay stable across a resume. A conversation can continue on a newer version, as the first topic showed, but the stored version tells you what produced the history.

## Hands-On Demo

[Plan, Reflect, Fork and Resume an Analyst Agent](demo-agentic-loops.md) runs the four loops against a market-data function tool and resumes the saved session from a second process. The runnable projects are `agentic-loops-py/` (Python) and `agentic-loops-cs/` (C#).

## Key Topics

- [Build with agents, conversations, and responses](https://learn.microsoft.com/azure/foundry/agents/concepts/runtime-components)
- [Function calling with Foundry agents](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/function-calling)
- [Streaming and background responses](https://learn.microsoft.com/azure/foundry/agents/concepts/runtime-components#streaming-and-background-responses)
- [Migrate to the Agents SDK](https://learn.microsoft.com/azure/foundry/agents/how-to/migrate)
