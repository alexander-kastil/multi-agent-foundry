# Call Models with the Foundry SDK and IChatClient

Sample code for calling a model comes in many shapes: raw REST, the OpenAI client with a key, a vendor SDK, a framework wrapper. Each one signs in differently and breaks when the endpoint changes. In this demo you call the same Foundry model three ways with one project endpoint and one credential, first in Python and then in C# through `IChatClient`.

What you practice here: reaching a model through the Foundry project client and its OpenAI-compatible client, and wrapping the same model behind `IChatClient` in .NET.

Duration: about 50 minutes.

## Prerequisites

- Python 3.10 or later, the .NET 10 SDK, and Azure CLI signed in with `az login`.
- Foundry User on the `pro-code-agents` project and a `gpt-5.4` deployment.

## Run it

The Python project lives in `foundry-sdk-py/` and the C# project in `foundry-sdk-cs/`. Both read `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` from a `.env` file.

```bash
cd demos/01-foundry-essentials/03-foundry-sdk/foundry-sdk-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
```

Copy the same `.env` into `../foundry-sdk-cs/` when you reach step 4.

## Step 1: Call the Responses API through the project client

The project client authenticates once and hands out an OpenAI-compatible client bound to the project. The Responses API takes the system prompt as `instructions` and the user turn as `input`.

```python
project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)
openai = project.get_openai_client()

response = openai.responses.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    instructions="You are an AI assistant that speaks like a techno punk rocker from 2350. Be cool but not too cool. Keep it under 60 words.",
    input="Hey, can you help me with my taxes? I'm a freelancer.",
)
```

```bash
.venv/Scripts/python responses_call.py
```

Expected output (the wording varies):

```text
model  : gpt-5.4
status : completed
tokens : 55 in, 72 out
answer : Yeah, chrome-heart, I can help with freelancer taxes. Hit me with your country/state, business structure, income, expenses, and whether you got 1099s or equivalent docs. I can break down deductions, estimated taxes, self-employment tax, and what records you need. Not legal/tax gospel—just sharp guidance.
```

`response.model` is the deployment's model and `status` is `completed`. There is no API key anywhere in the script.

## Step 2: Send the same request as Chat Completions

Existing code often speaks the messages format. The same OpenAI-compatible client serves it, so you can migrate gradually.

```python
completion = openai.chat.completions.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    messages=[
        {"role": "system", "content": "You are an AI assistant that speaks like a techno punk rocker from 2350. Be cool but not too cool. Keep it under 60 words."},
        {"role": "user", "content": "Hey, can you help me with my taxes? I'm a freelancer."},
    ],
)
```

```bash
.venv/Scripts/python chat_completions_call.py
```

Expected output (the wording varies):

```text
model  : gpt-5.4-2026-03-05
finish : stop
tokens : 55 in, 62 out
answer : Totally, byte-runner. I can help with freelancer taxes: income tracking, deductible expenses, estimated quarterly payments, 1099s, self-employment tax, home office, gear, mileage, the whole ledger-riff.

Hit me with your country/state and what you need most.
```

The two calls hit the same deployment. Note that Chat Completions reports the dated model name and Responses reports the deployment's model name, and that both count the same 55 input tokens.

## Step 3: Let the model call a Python function

Tool calling is a loop you own: the model returns a `function_call` item, you run the function, and you send the result back as a `function_call_output` item with the same `call_id`.

```python
for item in response.output:
    if item.type == "function_call":
        arguments = json.loads(item.arguments)
        result = lookup_weather(**arguments)
        input_items.append(
            {"type": "function_call_output", "call_id": item.call_id, "output": json.dumps(result)}
        )

final = openai.responses.create(
    model=model,
    instructions="You are a weather chatbot.",
    input=input_items,
    tools=tools,
)
```

```bash
.venv/Scripts/python function_calling.py
```

Expected output:

```text
tool call : lookup_weather({'city_name': 'Vienna'})
answer    : Yes — it’s sunny in Vienna, about 21°C.
```

The tool call line proves the model asked for your function with arguments it extracted from the question. The answer comes from the returned data, since the model has no weather knowledge of its own at this point.

## Step 4: Wrap the model as an IChatClient in C#

In .NET the same project endpoint feeds `IChatClient`. The application code below never mentions Foundry or OpenAI, so you can swap the provider without touching it.

```csharp
AIProjectClient project = new(endpoint, new DefaultAzureCredential());

IChatClient client = project
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(model);

ChatResponse response = await client.GetResponseAsync(Messages);
Console.WriteLine($"answer: {response.Text}");
```

```bash
cd ../foundry-sdk-cs
cp ../foundry-sdk-py/.env .env
dotnet run -- chat
```

Expected output (trimmed):

```text
model : gpt-5.4
answer: - Whey protein
- Casein protein
- Egg protein
- Soy protein
...
tokens: 89
```

`extensions-ai-cs/` is the shortest form of the same call: it builds the client with `GetProjectResponsesClientForModel(model).AsIChatClient(model)`, sends the system and user message once and prints the answer.

```bash
cd ../extensions-ai-cs
cp ../foundry-sdk-py/.env .env
dotnet run
```

```text
[ASSISTANT]: - Whey protein
- Casein protein
- Egg protein
...
```

If `dotnet run` fails with a `MissingMethodException`, the `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI` and `OpenAI` versions no longer match `Azure.AI.Extensions.OpenAI` 2.0.0, which is built against `OpenAI` 2.9.1. Both C# projects pin the 10.4.1 pair that resolves to that version; a newer `Microsoft.Extensions.AI.OpenAI` pulls a newer `OpenAI` and breaks the project client at startup. Pass the model to `AsIChatClient(model)` as well, otherwise the service answers `HTTP 400 missing_required_parameter: model`.

## Step 5: Stream the response

Streaming returns updates as the model produces them. Use `GetStreamingResponseAsync` and write each update as it arrives.

```csharp
await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync(Messages))
{
    Console.Write(update.Text);
}
```

```bash
dotnet run -- stream
```

Expected output (trimmed):

```text
answer: - Whey protein
- Casein protein
- Egg protein
...
```

The list appears incrementally in the terminal instead of all at once. The final text matches step 4 because the prompt is the same.

## Step 6: Give the client a tool with middleware

`ChatClientBuilder` wraps the client in middleware. `UseFunctionInvocation` runs the call loop that you wrote by hand in step 3, and `AIFunctionFactory.Create` turns a C# method into the tool definition.

```csharp
IChatClient client = new ChatClientBuilder(inner)
    .UseFunctionInvocation()
    .Build();

ChatOptions options = new()
{
    Tools = [AIFunctionFactory.Create(LookupWeather)]
};
```

```bash
dotnet run -- tools
```

Expected output:

```text
tool  : LookupWeather(cityName=Vienna, zipCode=)
answer: Yes — it’s sunny in Vienna, and about 21°C.
```

The `tool` line is printed by your C# method, so the model really called it. Compare it with step 3: the loop code disappeared and only the tool and the middleware remain.

## Verify

You have verified the demo when step 3 prints a `tool call` line and the answer mentions Vienna, and step 6 prints the `tool` line from C# before its answer. Both prove the model invoked your function instead of guessing.

## Cleanup

The demo creates no Azure resources and stores nothing in the project, so there is nothing to delete. The C# `bin/` and `obj/` folders are git-ignored build output.
