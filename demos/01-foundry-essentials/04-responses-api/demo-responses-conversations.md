# Build a Streaming Chat with Conversations

A chat that forgets the previous question is not a chat, and a chat that prints nothing until the whole answer is ready feels broken. The Responses API solves both: the service can hold the history for you, and it can stream the answer as it is produced. In this demo you build up to a small command-line chat that streams answers into a server-side conversation, without creating any agent resource.

What you practice here: carrying state with `previous_response_id`, `store=False` and conversations, and consuming the Responses event stream.

Duration: about 40 minutes.

## Prerequisites

- Python 3.10 or later or the .NET 10 SDK, and Azure CLI signed in with `az login`.
- Foundry User on the `pro-code-agents` project and a `gpt-5.4` deployment.

## Run it

The Python scripts live in `responses-conversations-py/` and the C# commands in `responses-conversations-cs/`.

```bash
cd demos/01-foundry-essentials/04-responses-api/responses-conversations-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
```

The C# commands in the steps run from this same folder with `--project ../responses-conversations-cs`, and .NET reads the `.env` of the folder you run from. Restore the C# project once:

```bash
dotnet restore ../responses-conversations-cs
```

## Step 1: Send one response and inspect what was stored

A response is stored by default, which is why you can fetch it by id later. `single_response.py` creates one, looks at its output items, retrieves it again and deletes it.

```python
response = openai.responses.create(
    model=model,
    instructions="You are a concise travel assistant.",
    input="Name one must-see place in Salzburg.",
)

print(f"output  : {[item.type for item in response.output]}")

stored = openai.responses.retrieve(response.id)
print(f"stored  : {stored.id == response.id}")

openai.responses.delete(response.id)
```

```csharp
ResponseResult response = await responses.CreateResponseAsync(new CreateResponseOptions
{
    Instructions = "You are a concise travel assistant.",
    InputItems = { ResponseItem.CreateUserMessageItem("Name one must-see place in Salzburg.") },
});

ResponseResult stored = await responses.GetResponseAsync(response.Id);
await responses.DeleteResponseAsync(response.Id);
```

```bash
.venv/Scripts/python single_response.py
dotnet run --project ../responses-conversations-cs -- single
```

Expected output (the id differs):

```text
id      : resp_05bcfc35b04471fa006ad46152f97881968d478f6f1051a2fe
status  : completed
output  : ['message']
text    : Hohensalzburg Fortress is a must-see in Salzburg.
stored  : True
deleted : True
```

No agent was created anywhere. The `instructions` travelled with the request, and the only service-side object is the response itself. The C# command prints the same lines, with `status  : Completed` capitalized by the .NET enum.

## Step 2: Chain turns with previous_response_id and carry them yourself

The first approach to memory points at the last response. The second keeps the service out of it with `store=False` and resends the earlier messages.

![Three rows comparing previous_response_id, a conversation and client-side history with store=False: what turn 2 sends and where the history lives](../pptx/images/04-01-carry-history.svg)

```python
second = openai.responses.create(
    model=model,
    input="Which city did I say was my favourite?",
    previous_response_id=first.id,
)

stateless_second = openai.responses.create(
    model=model,
    input=[
        {"role": "user", "content": "My favourite city is Graz. Remember that."},
        {"role": "assistant", "content": stateless_first.output_text},
        {"role": "user", "content": "Which city did I say was my favourite?"},
    ],
    store=False,
)
```

```csharp
ResponseResult second = await responses.CreateResponseAsync(new CreateResponseOptions
{
    PreviousResponseId = first.Id,
    InputItems = { ResponseItem.CreateUserMessageItem("Which city did I say was my favourite?") },
});

ResponseResult statelessFirst = await responses.CreateResponseAsync(new CreateResponseOptions
{
    StoredOutputEnabled = false,
    InputItems = { ResponseItem.CreateUserMessageItem("My favourite city is Graz. Remember that.") },
});
```

```bash
.venv/Scripts/python chain_responses.py
dotnet run --project ../responses-conversations-cs -- chain
```

Expected output:

```text
chained   : You said your favourite city is Graz.
stateless : You said your favourite city is Graz.
```

Both answers are right, and you pay for the history differently. The chained call sends only the new question, while the stateless call resends every earlier message and leaves nothing in the service.

## Step 3: Keep the history in a conversation

A conversation is a durable object that collects the input and output items of every response that references it. `conversation.py` creates one with a first message, asks two questions against it, appends a message manually and lists the items.

![A conversation with six items beside one turn: the service prepends the stored items, the model answers, and the new input and output are appended](../pptx/images/04-02-conversation-items.svg)

```python
conversation = openai.conversations.create(
    items=[{"type": "message", "role": "user", "content": "I am planning a trip from Vienna."}],
    metadata={"demo": "m1-conversation"},
)

response = openai.responses.create(
    model=model,
    conversation=conversation.id,
    instructions="Answer in one short sentence.",
    input=question,
)

for item in openai.conversations.items.list(conversation.id, order="asc"):
    print(f"{item.type:<10} {getattr(item, 'role', '-'):<10} {item.id}")
```

```csharp
ProjectConversationCreationOptions options = new()
{
    Items = { ResponseItem.CreateUserMessageItem("I am planning a trip from Vienna.") },
    Metadata = { ["demo"] = "m1-conversation" },
};
ProjectConversation conversation = await conversations.CreateProjectConversationAsync(options);

var responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(model, conversation.Id);

foreach (AgentResponseItem item in conversations.GetProjectConversationItems(conversation.Id, order: "asc"))
{
    Console.WriteLine(item.Id);
}
```

```bash
.venv/Scripts/python conversation.py
dotnet run --project ../responses-conversations-cs -- conversation
```

Expected output (the ids differ):

```text
conversation : conv_0bf907655f97446a00NC6pZT8lmNlNK7u1yOLP35l50PpiEdgP
user         : Which country is my departure city in?
assistant    : Vienna is in Austria.
user         : And which river runs through it?
assistant    : The Danube runs through Vienna.

message    user       msg_0bf907655f97446a00YaljeImnJk7BzAYeIJSlrxuWGDXS5G33
message    user       msg_0bf907655f97446a006ad4615d0b6c8190bf3928126e539350
message    assistant  msg_0bf907655f97446a006ad4615d6d6c8190baa10274b0b01fa5
message    user       msg_0bf907655f97446a006ad4615e40208190b7bd69fbfa00d764
message    assistant  msg_0bf907655f97446a006ad4615e8b1881908d93b09b38b7cc25
message    user       msg_0bf907655f97446a005k7DI37pq6TnpQCctnuNceAZValEvKQk

conversation deleted
```

In C# the conversation id goes into the responses client, so every call through it joins the conversation. The model knew the departure city only because the conversation carried it. Six items are stored in order: the opening message, two question and answer pairs, and the message you appended by hand. The script deletes the conversation at the end.

## Step 4: Read the event stream

With `stream=True` the call returns events. `stream_events.py` prints the text deltas as they arrive and records the event types and the time to the first token.

![The nine streaming event types in order with their .NET update classes, and a bar with the first token at 1.75 s and completion at 2.63 s](../pptx/images/04-03-streaming-events.svg)

```python
stream = openai.responses.create(model=model, input="Explain in three sentences why streaming improves perceived latency.", stream=True)

for event in stream:
    if event.type == "response.output_text.delta":
        print(event.delta, end="", flush=True)
    if event.type == "response.completed":
        usage = event.response.usage
```

```csharp
await foreach (StreamingResponseUpdate update in responses.CreateResponseStreamingAsync(
    "Explain in three sentences why streaming improves perceived latency."))
{
    if (update is StreamingResponseOutputTextDeltaUpdate delta)
    {
        Console.Write(delta.Delta);
    }
}
```

```bash
.venv/Scripts/python stream_events.py
dotnet run --project ../responses-conversations-cs -- stream
```

Expected output (text and timings vary):

```text
Streaming improves perceived latency by sending partial results as soon as they are available, so users see progress immediately instead of waiting for the full response. This reduces the apparent wait time because people can start reading or interacting with the output before generation is complete. It also improves the user experience by making the system feel faster and more responsive, even if total completion time stays the same.

event types : ['response.created', 'response.in_progress', 'response.output_item.added', 'response.content_part.added', 'response.output_text.delta', 'response.output_text.done', 'response.content_part.done', 'response.output_item.done', 'response.completed']
first token : 1.75s
completed   : 2.63s (78 output tokens)
```

The first token arrived about a second before the answer completed, and that gap is the latency a user does not wait through. Only `response.output_text.delta` carries text, so a client can ignore the other types.

The .NET SDK delivers each event as a typed update class, so the C# command lists the class names instead of the wire strings:

```text
event types : [Created, InProgress, OutputItemAdded, ContentPartAdded, OutputTextDelta, OutputTextDone, ContentPartDone, OutputItemDone, Completed]
first token : 1.79s
completed   : 2.67s (78 output tokens)
```

## Step 5: Stream into a conversation from a chat loop

`chat.py` combines both ideas. It creates a conversation, sends every command-line argument as a user turn with `stream=True`, prints the deltas and finally counts the items the service stored.

```python
conversation = openai.conversations.create(metadata={"demo": "m1-chat"})

for question in sys.argv[1:]:
    stream = openai.responses.create(
        model=model,
        conversation=conversation.id,
        instructions="You are a friendly assistant. Answer in at most two sentences.",
        input=question,
        stream=True,
    )
    for event in stream:
        if event.type == "response.output_text.delta":
            print(event.delta, end="", flush=True)
```

```csharp
await foreach (StreamingResponseUpdate update in responses.CreateResponseStreamingAsync(new CreateResponseOptions
{
    Instructions = "You are a friendly assistant. Answer in at most two sentences.",
    InputItems = { ResponseItem.CreateUserMessageItem(question) },
}))
{
    if (update is StreamingResponseOutputTextDeltaUpdate delta)
    {
        Console.Write(delta.Delta);
    }
}
```

```bash
.venv/Scripts/python chat.py "My name is Alex and I live in Vienna." "What is my name and which city do I live in?" "Suggest a coffee house there."
dotnet run --project ../responses-conversations-cs -- chat "My name is Alex and I live in Vienna." "What is my name and which city do I live in?" "Suggest a coffee house there."
```

Expected output (the answers vary):

```text
conversation: conv_0dc572f0ce1dc944001OFTyZ0U5BuVnLRywqNbr5aJzWZsjHY6

you: My name is Alex and I live in Vienna.
bot: Nice to meet you, Alex! Vienna is a beautiful city.

you: What is my name and which city do I live in?
bot: Your name is Alex, and you live in Vienna.

you: Suggest a coffee house there.
bot: Try Café Central in Vienna—it’s iconic and beautiful. If you want, I can suggest a quieter or less touristy option too.

items stored: 6
conversation deleted
```

Turns two and three only work because the streamed turns were appended to the conversation when each response completed. Six items means three user inputs and three assistant outputs.

## Verify

You have verified the demo when step 3 lists six items with the assistant answering the second question from the first message, and step 5 prints `items stored: 6` followed by `conversation deleted`.

## Cleanup

Every script and C# command deletes what it creates: step 1 deletes its response, step 2 deletes the chained responses, and steps 3 and 5 delete their conversation. The unchained `store=False` calls were never stored. The C# `bin/` and `obj/` folders are git-ignored build output.
