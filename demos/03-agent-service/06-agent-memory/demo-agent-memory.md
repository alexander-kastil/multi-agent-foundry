# Give an Agent a Memory and Teach It a Routine

An agent that forgets everything between conversations makes people repeat themselves: the coffee order, the allergy, the way your team publishes release notes. In this demo you create a memory store, feed it a conversation, search it and then attach it to an agent that recalls one user's preference and knows nothing about another user's. You finish with a working scoped memory and a clean project.

## Prerequisites

- A Microsoft Foundry project with a `gpt-5.4` chat deployment and a `text-embedding-3-small` embedding deployment, and `az login` completed.
- The project must have a system-assigned managed identity with the Foundry User role on the Foundry resource that hosts the project. `provision.azcli` creates that assignment.
- Owner or User Access Administrator on the Foundry resource, to create the role assignment.
- Python 3.12 or later, or the .NET 10 SDK for the C# project.
- Memory is a preview feature. The guide pins `azure-ai-projects` 2.9.0 for Python and `Azure.AI.Projects` 2.0.1 for C#.

## Run it

```bash
cd demos/03-agent-service/06-agent-memory
bash provision.azcli
cd agent-memory-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python agent_memory.py
```

Edit the three variables at the top of `provision.azcli` first, and set `PROJECT_ENDPOINT` in `.env`.

The C# project in `agent-memory-cs/` runs the same six steps and prints the same section headings:

```bash
cd demos/03-agent-service/06-agent-memory/agent-memory-cs
cp .env.example .env
dotnet run
```

Run one project at a time; both use the store `m3-memory-store` and clean it up at the end. A role assignment can take a few minutes to take effect.

## Step 1: Create a memory store

The memory store names the chat model that extracts memories and the embedding model that indexes them. It enables all three memory types, expires entries after a day and tells the extractor which kinds of data to ignore.

```python
memory_stores.create(
    name=STORE,
    definition=MemoryStoreDefaultDefinition(
        chat_model=MODEL,
        embedding_model=os.environ["EMBEDDING_MODEL_DEPLOYMENT_NAME"],
        options=MemoryStoreDefaultOptions(
            user_profile_enabled=True,
            chat_summary_enabled=True,
            procedural_memory_enabled=True,
            default_ttl_seconds=timedelta(days=1),
            user_profile_details="Avoid irrelevant or sensitive data, such as age, financials, precise location and credentials",
        ),
    ),
    description="Module 3 memory demo",
)
```

The C# typed `MemoryStoreDefaultOptions` in `Azure.AI.Projects` 2.0.1 exposes only `IsUserProfileEnabled`, `IsChatSummaryEnabled` and `UserProfileDetails`. It has no procedural memory flag and no TTL, so the C# project sends the same JSON the Python SDK builds through the protocol overload of `CreateMemoryStoreAsync`.

```csharp
string definition = JsonSerializer.Serialize(new
{
    name = store,
    description = "Module 3 memory demo",
    definition = new
    {
        kind = "default",
        chat_model = model,
        embedding_model = embeddingModel,
        options = new
        {
            user_profile_enabled = true,
            chat_summary_enabled = true,
            procedural_memory_enabled = true,
            default_ttl_seconds = 86400,
            user_profile_details = "Avoid irrelevant or sensitive data, such as age, financials, precise location and credentials",
        },
    },
});
await memoryStores.CreateMemoryStoreAsync(BinaryContent.Create(BinaryData.FromString(definition)));
```

Expected output:

```text
== Step 1: create a memory store
Created m3-memory-store
```

If the next step fails with an authentication error from the model resource, the project identity is missing the Foundry User role on the Foundry resource. Run `provision.azcli` and retry after a few minutes.

## Step 2: Write memories from a conversation

The conversation holds a preference, an allergy and a correction. The assistant claims it published release notes, and the user replies with the real process. `begin_update_memories` extracts and consolidates memories for the scope `alice`, and `update_delay=0` runs it immediately.

![A conversation with a preference, an allergy and a correction passes through extraction and consolidation in m3-memory-store, lands in scope alice as user_profile, chat_summary and procedural memories, and comes back through a static and a contextual search_memories call](../pptx/images/06-01-three-memory-phases.svg)

```python
result = memory_stores.begin_update_memories(name=STORE, scope="alice", items=conversation_items, update_delay=0).result()
for operation in result.memory_operations:
    print(f"  {operation.kind} [{operation.memory_item.kind}] {operation.memory_item.content[:150]}")
```

In C# the same call is `UpdateMemoriesAsync` followed by a poll of `GetUpdateResultAsync`. `WaitForMemoriesUpdateAsync` is not used because it rejects assistant messages on the client side, and this conversation needs them.

```csharp
MemoryUpdateOptions update = new("alice") { UpdateDelay = 0, Items = { ResponseItem.CreateUserMessageItem("..."), ResponseItem.CreateAssistantMessageItem("...") } };
MemoryUpdateResult result = (await memoryStores.UpdateMemoriesAsync(store, update)).Value;
while (result.Status is MemoryStoreUpdateStatus.Queued or MemoryStoreUpdateStatus.InProgress)
{
    await Task.Delay(2000);
    result = (await memoryStores.GetUpdateResultAsync(store, result.UpdateId)).Value;
}
```

Expected output (the wording is model-generated and varies):

```text
== Step 2: write memories from a conversation
  created [user_profile] The user prefers dark roast coffee.
  created [user_profile] The user is allergic to peanuts.
  created [user_profile] The user follows a standard release-notes publishing workflow: run the changelog script first, then get approval from the release manager in the revie
  created [chat_summary] The user shared two lasting personal preferences and constraints: the user prefers dark roast coffee and the user is allergic to peanuts. The peanut a
  created [chat_summary] The conversation established the correct workflow for publishing release notes. The user asked for release notes for version 2.4 to be published. The 
  created [procedural] {"applicable_to":"When asked to carry out a multi-step publishing or release process that has required approvals or sequencing constraints.","instruct
```

The call takes about half a minute. All three memory types appear. The procedural memory is a small JSON object with `applicable_to` and `instruction` fields, learned from the moment the user corrected the assistant.

## Step 3: Search static and contextual memories

A search with only a scope returns the static user profile. A search with `items` returns the memories most relevant to the new message, including chat summaries and the procedural rule.

```python
memory_stores.search_memories(name=STORE, scope="alice").memories
memory_stores.search_memories(
    name=STORE,
    scope="alice",
    items=[message("user", "Publish the release notes for 2.6")],
    options=MemorySearchOptions(max_memories=5),
).memories
```

```csharp
await memoryStores.SearchMemoriesAsync(store, new MemorySearchOptions("alice"));
await memoryStores.SearchMemoriesAsync(store, new MemorySearchOptions("alice")
{
    Items = { ResponseItem.CreateUserMessageItem("Publish the release notes for 2.6") },
    ResultOptions = new MemorySearchResultOptions { MaxMemories = 5 },
});
```

Expected output:

```text
== Step 3: search static and contextual memories
static memories for alice:
  [user_profile] The user prefers dark roast coffee.
  [user_profile] The user is allergic to peanuts.
  [user_profile] The user follows a standard release-notes publishing workflow: run the changelog script first, then get approval from the release manager in the revie
contextual memories for 'Publish the release notes for 2.6':
  [user_profile] The user follows a standard release-notes publishing workflow: run the changelog script first, then get approval from the release manager in the revie
  [chat_summary] The conversation established the correct workflow for publishing release notes. The user asked for release notes for version 2.4 to be published. The 
  [procedural] {"applicable_to":"When asked to carry out a multi-step publishing or release process that has required approvals or sequencing constraints.","instruct
  [chat_summary] The user shared two lasting personal preferences and constraints: the user prefers dark roast coffee and the user is allergic to peanuts. The peanut a
  [user_profile] The user is allergic to peanuts.
memories visible to bob: 0
```

The last line is the scope boundary. Bob searches the same store and sees nothing, because every memory belongs to the scope `alice`.

## Step 4: Attach the memory search tool to an agent

The memory search tool lets an agent read and write the store during a conversation. The scope `{{$userId}}` resolves per request from the `x-memory-user-id` header, so one agent version serves every user. `update_delay=1` shortens the write debounce for the demo; the default is 300 seconds.

```python
agent = project.agents.create_version(
    agent_name=AGENT,
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="You are a helpful assistant. Use what you remember about the user and say when you remember nothing.",
        tools=[MemorySearchPreviewTool(memory_store_name=STORE, scope="{{$userId}}", update_delay=1)],
    ),
)

def ask(user, conversation, text):
    return openai.responses.create(
        input=text,
        conversation=conversation.id,
        extra_headers={"x-memory-user-id": user},
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    )
```

The C# tool is `MemorySearchPreviewTool` on a `DeclarativeAgentDefinition`. The SDK has no per-call header argument, so the `x-memory-user-id` header is added by a small `PipelinePolicy` on a client built per user.

```csharp
ProjectsAgentVersion agent = (await project.AgentAdministrationClient.CreateAgentVersionAsync(
    agentName: agentName,
    options: new(new DeclarativeAgentDefinition(model)
    {
        Instructions = "You are a helpful assistant. Use what you remember about the user and say when you remember nothing.",
        Tools = { new MemorySearchPreviewTool(store, "{{$userId}}") { UpdateDelayInSecs = 1 } },
    }))).Value;

ProjectOpenAIClientOptions options = new();
options.AddPolicy(new HeaderPolicy("x-memory-user-id", user), PipelinePosition.PerCall);
ProjectResponsesClient client = new ProjectOpenAIClient(endpoint, credential, options)
    .GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version), conversation.Id);
```

Expected output:

```text
== Step 4: attach the memory search tool to an agent
Created m3-memory-agent:1
carol, conversation 1: I prefer oat milk in my flat white
agent: Got it ... you prefer oat milk in your flat white.
waiting for memories to be written
```

The script waits 65 seconds after this first conversation so the service can extract and store the preference.

## Step 5: Recall in a new conversation and prove isolation

Two users ask the same question in brand-new conversations. Carol has a stored preference and Dave does not, so the same agent version answers differently.

```python
for user in ("carol", "dave"):
    fresh = openai.conversations.create()
    print(f"agent: {ask(user, fresh, 'Please order my usual coffee.').output_text}")
```

Expected output:

```text
== Step 5: recall in a new conversation and prove scope isolation
carol, new conversation: Please order my usual coffee
agent: I remember your usual coffee preference: a flat white with oat milk.

I can't actually place orders, but you could order:
"A flat white with oat milk, please."
dave, new conversation: Please order my usual coffee
agent: I don't have any saved memory about your usual coffee.
```

Carol's second conversation never contained the words oat milk. The memory came from the store, and Dave's request used a different scope.

![carol and dave ask one agent version the same question with their own x-memory-user-id header; the scope {{$userId}} resolves to scope carol, which holds the oat milk preference, and to scope dave, which is empty, so only carol gets the remembered answer](../pptx/images/06-02-memory-scope.svg)

## Step 6: Remember and forget on command

A user can say "remember" or "forget", and the memory search tool applies it immediately. The response carries a command item, so an application can show or audit exactly what changed.

```python
tools = [{"type": "memory_search_preview", "memory_store_name": STORE, "scope": "erin"}]
for text in ("Remember that my preferred seat is aisle.", "Forget my preferred seat."):
    response = openai.responses.create(model=MODEL, tools=tools, input=text)
    for item in response.output:
        if item.type == "memory_command_preview_call":
            print(f"{text} -> {item.arguments} ({item.status})")
```

The typed C# response model has no item for the memory command, so the C# project posts the request body through the protocol overload and reads the output as JSON.

```csharp
string body = JsonSerializer.Serialize(new
{
    model,
    input = new[] { new { type = "message", role = "user", content = text } },
    tools = new[] { new { type = "memory_search_preview", memory_store_name = store, scope = "erin" } },
});
ClientResult raw = await direct.CreateResponseAsync(BinaryContent.Create(BinaryData.FromString(body)));
```

Expected output:

```text
== Step 6: remember and forget on command
Remember that my preferred seat is aisle. -> {"action":"remember","content":"The user's preferred seat is aisle."} (completed)
Forget my preferred seat. -> {"action":"forget","content":"The user's preferred seat is aisle."} (completed)
```

The documentation names this item `memory_command_call`. The service returned `memory_command_preview_call` in this run, so match on the prefix in code you plan to keep.

## Verify

Alice's store holds user profile, chat summary and procedural memories, Bob sees none of them, Carol's preference survives into a new conversation and Dave gets no preference. The remember and forget commands both report `completed`.

The C# project prints the same sections. A trimmed run:

```text
== Step 2: write memories from a conversation
  created [user_profile] The user prefers dark roast coffee.
  created [procedural] {"applicable_to":"When asked to publish or complete a workflow-driven release task without evidence that prerequisite steps wer
== Step 3: search static and contextual memories
memories visible to bob: 0
== Step 5: recall in a new conversation and prove scope isolation
carol, new conversation: Please order my usual coffee
agent: I can help draft the order, but I can't place it myself. Your usual coffee, based on what I remember, is a flat white with oat milk.
dave, new conversation: Please order my usual coffee
agent: I don't remember your usual coffee order. Tell me what you want, and I can help you place it or draft the order message.
== Step 6: remember and forget on command
Remember that my preferred seat is aisle. -> {"action":"remember","content":"Your preferred seat is aisle."} (completed)
Forget my preferred seat. -> {"action":"forget","content":"Your preferred seat is aisle."} (completed)
== Cleanup
Deleted scopes, agent version and memory store
```

## Cleanup

Both projects delete every scope, the agent version and the memory store at the end. To remove one user's data, for example to answer a deletion request:

```python
memory_stores.delete_scope(name="m3-memory-store", scope="carol")
```

Remove the role assignment when you no longer need memory in the project:

```bash
bash deprovision.azcli
```
