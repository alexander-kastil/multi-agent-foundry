# Measure, Trim, Compact and Cache a Long Context

An agent that calls tools gets slower and more expensive with every turn, because each call resends everything that happened before. In this demo you grow a tool-heavy conversation, read its token budget, see what trimming loses, compact it, let the service compact during a loop and finally prove that a stable prefix is served from the prompt cache.

What you practice here: reading `usage`, trimming by turn, `responses.compact`, server-side `context_management` and `prompt_cache_key` with `cached_tokens`.

Duration: about 45 minutes.

## Prerequisites

- Python 3.10 or later or the .NET 10 SDK, and Azure CLI signed in with `az login`.
- Foundry User on the `pro-code-agents` project and a `gpt-5.4` deployment.
- The Responses basics from [Responses API: Conversations & Streaming](../04-responses-api/readme.md).

## Run it

The Python scripts live in `context-engineering-py/` and the C# commands in `context-engineering-cs/`.

```bash
cd demos/01-foundry-essentials/06-context-engineering/context-engineering-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
```

The C# commands run from this same folder with `--project ../context-engineering-cs`, and .NET reads the `.env` of the folder you run from. Restore the C# project once:

```bash
dotnet restore ../context-engineering-cs
```

Every request in this demo sets `store=False`, so nothing is saved in the project and there is nothing to clean up in Foundry.

## The history the steps share

`history.py` builds a conversation of weather questions. Each day is one turn with four items: a user message, a `function_call` to `get_forecast`, a `function_call_output` with a 24-line forecast and an assistant message. The first turn plants the fact the steps keep asking for: a booking reference and a travel companion.

```python
def turn(day):
    city = CITIES[day % len(CITIES)]
    return [
        {"role": "user", "content": f"{first}Day {day}: what is the weather in {city}?"},
        {"type": "function_call", "call_id": f"call_{day}", "name": "get_forecast", "arguments": json.dumps({"city": city, "day": day}), "status": "completed"},
        {"type": "function_call_output", "call_id": f"call_{day}", "output": forecast(day)},
        {"type": "message", "role": "assistant", "status": "completed", "content": [{"type": "output_text", "text": f"Day {day} in {city} is mostly {SKIES[day % len(SKIES)]}, so plan indoor stops for the afternoon."}]},
    ]
```

The C# project builds the same items as `System.Text.Json.Nodes` in `History.cs`. It uses JSON for every step because the .NET SDK has no typed surface for compaction, `context_management` or `prompt_cache_key`.

## Step 1: Read the budget

`budget.py` sends the same question after 2, 10 and 20 days of history and prints what each request cost. The answer is always right, so the only thing that changes is the price.

```python
response = openai.responses.create(
    model=model,
    instructions="Answer in one short sentence.",
    input=[*flatten(build_turns(days)), {"role": "user", "content": QUESTION}],
    store=False,
    max_output_tokens=2000,
)
usage = response.usage
print(f"{days:>3} days : {usage.input_tokens:>6} input tokens, {usage.output_tokens:>3} output tokens")
```

```csharp
var response = await Raw.Create(new()
{
    ["instructions"] = "Answer in one short sentence.",
    ["input"] = input,
    ["store"] = false,
    ["max_output_tokens"] = 2000,
});
Console.WriteLine($"{days,3} days : {Raw.Tokens(response, "input_tokens"),6} input tokens");
```

```bash
.venv/Scripts/python budget.py
dotnet run --project ../context-engineering-cs -- budget
```

Expected output:

```text
  2 days :   1170 input tokens,  22 output tokens
         Your booking reference is ZX-4417, and you’re travelling with your sister Marta.
 10 days :   5679 input tokens,  22 output tokens
         Your booking reference is ZX-4417, and you’re travelling with your sister Marta.
 20 days :  11313 input tokens,  22 output tokens
         Your booking reference is ZX-4417, and you’re travelling with your sister Marta.
```

The output stays at 22 tokens while the input grows by about 570 tokens per day. Context cost grows with the length of the history, not with the size of the answer, and the C# command prints the same lines.

## Step 2: Trim by turn and see what you lose

`trim.py` keeps only the last four turns. Each turn stays whole, so no `function_call_output` is left without its call.

```python
turns = build_turns(20)

for name, kept in (("all 20 turns", turns), ("last 4 turns", turns[-4:])):
    response = openai.responses.create(
        model=model,
        instructions="Answer in one short sentence. If you do not know, say so.",
        input=[*flatten(kept), {"role": "user", "content": QUESTION}],
        store=False,
        max_output_tokens=2000,
    )
```

```csharp
var turns = History.BuildTurns(20);
var variants = new[] { ("all 20 turns", turns), ("last 4 turns", turns.TakeLast(4).ToList()) };
```

```bash
.venv/Scripts/python trim.py
dotnet run --project ../context-engineering-cs -- trim
```

Expected output:

```text
all 20 turns :  11322 input tokens
               Your booking reference is ZX-4417, and you are travelling with your sister Marta.
last 4 turns :   2287 input tokens
               I don’t know.
```

Trimming cut the request by about 80 percent and removed the one fact the user needs. The booking reference was stated in turn 1, so any trim that drops turn 1 forgets it, and that is the gap compaction closes.

## Step 3: Compact the history

`compact.py` sends the full 20-turn history to the standalone compaction endpoint and continues from the returned window. It prints the item types before and after so you can see what the service replaced.

```python
compacted = openai.responses.compact(model=model, input=history)

window = [item.model_dump(exclude_none=True, warnings=False) for item in compacted.output]

response = openai.responses.create(
    model=model,
    input=[*window, {"role": "user", "content": QUESTION}],
    store=False,
)
```

The C# SDK has no typed compaction call. `ResponsesClient.CompactResponseAsync` is a protocol method that takes the content type and a `BinaryContent` JSON body, which is what Learn recommends for .NET.

```csharp
JsonObject body = new() { ["model"] = Session.Model, ["input"] = input };
ClientResult result = await Session.Responses().CompactResponseAsync(
    "application/json", Content(body), new RequestOptions());
JsonNode compacted = JsonNode.Parse(result.GetRawResponse().Content)!;
```

```bash
.venv/Scripts/python compact.py
dotnet run --project ../context-engineering-cs -- compact
```

Expected output (the token counts vary from run to run):

```text
items before : {'message': 40, 'function_call': 20, 'function_call_output': 20}
items after  : {'message': 40, 'compaction': 1}
compact call : 11305 in, 404 out
next turn    : 1259 input tokens
answer       : Your booking reference is ZX-4417, and you’re travelling with your sister Marta.
```

The 40 messages survive and the 40 tool items collapse into one encrypted `compaction` item, so the next turn costs 1,259 input tokens instead of 11,322 and still knows the booking reference. The C# command prints the same lines with `{message: 40, ...}` formatting.

The compaction call itself reads the whole history once, so it pays off when you reuse the compacted window for many turns. Run the script a few times: the compaction item is sometimes larger, and occasionally close to the original size, so measure before you rely on a ratio.

## Step 4: Let the service compact during a loop

You do not have to call the endpoint yourself. `server_compaction.py` adds `context_management` to every request, with a `compact_threshold` of 6,000 tokens, and keeps going as an agent loop would. After each response it appends the output items and drops everything before the latest `compaction` item, which is the documented rule for stateless chaining.

```python
response = openai.responses.create(
    model=model,
    instructions="Answer in one short sentence.",
    input=conversation,
    store=False,
    max_output_tokens=2000,
    context_management=[{"type": "compaction", "compact_threshold": THRESHOLD}],
)
conversation += [item.model_dump(exclude_none=True) for item in response.output]
compactions = [k for k, item in enumerate(conversation) if item.get("type") == "compaction"]
if compactions:
    conversation = conversation[compactions[-1]:]
```

```csharp
["context_management"] = new JsonArray(new JsonObject { ["type"] = "compaction", ["compact_threshold"] = Threshold }),
```

```bash
.venv/Scripts/python server_compaction.py
dotnet run --project ../context-engineering-cs -- server
```

Expected output:

```text
day 9:   5076 input tokens, output ['message'], window now 36 items
day 10:   5642 input tokens, output ['message'], window now 40 items
day 11:   7024 input tokens, output ['compaction', 'message'], window now 2 items
day 12:   1847 input tokens, output ['message'], window now 6 items
day 13:   2418 input tokens, output ['message'], window now 10 items
day 14:   3007 input tokens, output ['message'], window now 15 items
Your booking reference is ZX-4417, and you’re travelling with your sister Marta.
```

The window grows until day 11 crosses 6,000 tokens. The service then emits a `compaction` item, prunes the context before it continues and returns to about 1,800 tokens, and from there it grows again at the same rate.

Because the loop used `store=False`, the compaction item travels in your own `input` array. If you chain with `previous_response_id` instead, you send only the new user message and do not prune by hand.

## Step 5: Cache the stable prefix

Compaction shrinks what you send. Prompt caching makes what you keep cheaper. `caching.py` builds a handbook of about 2,000 tokens, asks three questions against the same instructions and then moves the question in front of the handbook.

```python
response = openai.responses.create(
    model=model,
    instructions=instructions,
    input=question,
    max_output_tokens=2000,
    prompt_cache_key=f"m1-handbook-{run}",
    prompt_cache_retention="24h",
    store=False,
)
print(response.usage.input_tokens_details.cached_tokens)
```

```csharp
["prompt_cache_key"] = $"m1-handbook-{run}",
["prompt_cache_retention"] = "24h",
```

The run id at the top of the handbook only makes each run start cold, so the first call is a miss every time you run the script.

```bash
.venv/Scripts/python caching.py
dotnet run --project ../context-engineering-cs -- cache
```

Expected output:

```text
stable prefix, call 1 :  2001 input,     0 cached
stable prefix, call 2 :  2002 input,  1792 cached
stable prefix, call 3 :  2001 input,  1792 cached
variable text first   :  2010 input,     0 cached
```

Call 1 writes the cache, and calls 2 and 3 read 1,792 of about 2,000 tokens from it, which is the 1,024-token minimum plus six 128-token steps. The last request has the same key and the same handbook, but the question comes first, so the first 1,024 tokens differ and nothing is cached.

The C# command prints the same pattern with token counts a few tokens apart.

`gpt-5.4` accepts `prompt_cache_retention="24h"`, which selects extended retention. GPT-5.6 and later families deprecate that field and use `prompt_cache_options` instead, so check the model before you reuse the request.

## Verify

You have verified the demo when step 2 answers `I don’t know.` for the trimmed history, step 3 answers correctly from a window of roughly 1,300 input tokens, step 4 shows a `compaction` item in the output on day 11, and step 5 shows 1,792 cached tokens on calls 2 and 3 and 0 when the question comes first.

## Cleanup

Nothing was created in Foundry: every request used `store=False` and the compaction call is stateless. The `.venv/`, `bin/` and `obj/` folders and the `.env` files are git-ignored.
