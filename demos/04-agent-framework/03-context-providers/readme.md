# Context Providers & Long-Term Memory

A model only knows what is in the request. Context providers are the hooks that build that request: they run before each model call to add messages, instructions or tools, and after it to store what happened. Conversation history, user preferences and long-term memory are all context providers, which is why one list on the agent controls all three.

## The provider lifecycle

You attach providers with `context_providers=[...]` when you create the agent. Each provider has a unique `source_id`, a `before_run` hook and an `after_run` hook, and the agent runs them in list order.

```python
from agent_framework import Agent, InMemoryHistoryProvider

agent = Agent(
    client=client,
    name="MemoryBot",
    instructions="You are a helpful assistant.",
    context_providers=[InMemoryHistoryProvider("memory", load_messages=True)],
)

session = agent.create_session()
await agent.run("Remember that I prefer vegetarian food.", session=session)
```

Inside a hook, the `SessionContext` is where a provider contributes. `context.extend_instructions(source_id, text)` adds instructions, `context.extend_messages(...)` adds messages, `context.extend_tools(source_id, tools)` adds tools for this call, and `context.extend_middleware(source_id, middleware)` adds chat or function middleware. Provider-scoped `state` lives inside `AgentSession.state`, so it survives `to_dict()` and `from_dict()` without extra work.

## History providers

A `HistoryProvider` is a context provider that loads and stores the message transcript. `InMemoryHistoryProvider` is the built-in one; it keeps messages in the session state. Three flags shape its behavior.

| Flag | Meaning |
|---|---|
| `load_messages` | Replay stored messages before the model call. When `False` the provider only stores |
| `store_context_messages` | Also store messages other providers injected |
| `store_context_from` | Limit the stored context to specific `source_id` values |

Only one history provider should use `load_messages=True`. A second one with `load_messages=False, store_context_messages=True` makes a useful audit trail: it records the enriched prompt without changing what the agent replays.

```python
primary = InMemoryHistoryProvider("memory", load_messages=True)
audit = InMemoryHistoryProvider("audit", load_messages=False, store_context_messages=True)
agent = Agent(client=client, context_providers=[primary, audit])
```

The agent may add `InMemoryHistoryProvider()` with the source id `in_memory` on its own in some local-session cases. Add it explicitly when you want deterministic behavior. History from the Responses API normally lives on the service; pass `options={"store": False}` when you want a local history provider to own the transcript.

## A custom context provider

Subclass `ContextProvider` when you need your own enrichment. This provider remembers facts the user states and feeds them back as instructions.

```python
class PreferenceProvider(ContextProvider):
    def __init__(self) -> None:
        super().__init__("preferences")

    async def before_run(self, *, agent, session, context, state) -> None:
        if preferences := state.get("preferences"):
            context.extend_instructions(self.source_id, f"Known user preferences: {preferences}.")

    async def after_run(self, *, agent, session, context, state) -> None:
        for message in context.input_messages:
            ...
```

Keep per-session data in `state`, never on the provider instance, because one instance serves every session. Store only JSON-friendly values in `state`; custom classes must be registered with `register_state_type`.

## Persistent history stores

For history that must outlive the process, implement `HistoryProvider.get_messages` and `save_messages`, or use a packaged provider.

| Store | Class | Package | Status |
|---|---|---|---|
| Local files, one JSONL file per session | `FileHistoryProvider` | `agent-framework-core` | Experimental |
| Redis | `RedisHistoryProvider` | `agent-framework-redis` | Beta |
| Azure Cosmos DB | `CosmosHistoryProvider` | `agent-framework-azure-cosmos` | Beta |

```python
from agent_framework import FileHistoryProvider

agent = Agent(client=client, context_providers=[FileHistoryProvider("history")])
session = AgentSession(session_id="alex-trip")
await agent.run("Hello", session=session, options={"store": False})
```

The Redis and Cosmos packages are published as pre-releases (`1.0.0b261015` at the time of writing), so install them with `pip install agent-framework-redis --pre` or `pip install agent-framework-azure-cosmos --pre`. `RedisHistoryProvider` accepts `redis_url` or host and credentials plus `max_messages`; `CosmosHistoryProvider` accepts `endpoint`, `database_name`, `container_name` and a credential.

## Long-term memory with Foundry

History replays a transcript. Memory extracts durable facts from conversations and recalls only the relevant ones later, across sessions. `FoundryMemoryProvider` connects an agent to a Foundry memory store, a project-scoped item that Learn documents as a preview feature.

```python
from agent_framework.foundry import FoundryChatClient, FoundryMemoryProvider

memory = FoundryMemoryProvider(
    project_client=project,
    memory_store_name="m4-trail-memory",
    scope="user-alex",
    update_delay=0,
)
agent = Agent(
    client=FoundryChatClient(project_client=project, model="gpt-5.4"),
    context_providers=[memory],
    default_options={"store": False},
)
```

The store is created once with a chat model and an embedding model. The `scope` isolates memories, for example per user; when it is omitted the session ID is used, so memories stay inside one session. Extraction is asynchronous, so a fact told in one session can take several seconds to be recallable in the next, and `update_delay` (300 seconds by default) controls how long the provider waits before it sends updates.

The project's managed identity calls both models on your behalf. Learn requires the Foundry User role for that identity on the resource that contains the project; without it, memory writes and searches fail with a 401 against the embedding deployment.

## Other memory choices

`FileMemoryProvider` gives the model `file_memory_*` tools and stores notes through an `AgentFileStore`; a stable `scope` such as a user ID shares them across sessions. Mem0, Neo4j, Redis, Azure Cosmos DB and Azure AI Search each have an integration page for memory or retrieval patterns. Pick the provider by what you need to recall: an exact transcript, extracted facts, or documents.

## The same pieces in C#

In .NET the two base classes are `ChatHistoryProvider` for history and `AIContextProvider` for enrichment. Both are attached through `ChatClientAgentOptions`, and session-specific data goes into the session through `ProviderSessionState<T>`.

```csharp
ChatClientAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "TrailGuide",
    ChatOptions = new() { Instructions = "You are a trail guide." },
    ChatHistoryProvider = new InMemoryChatHistoryProvider(),
    AIContextProviders = [new PreferenceProvider()]
});

var history = agent.GetService<InMemoryChatHistoryProvider>()!.GetMessages(session);
```

An `AIContextProvider` overrides `ProvideAIContextAsync` to add context and `StoreAIContextAsync` to extract state. A `ChatHistoryProvider` overrides `ProvideChatHistoryAsync` and `StoreChatHistoryAsync`. `FoundryMemoryProvider` exists in `Microsoft.Agents.AI.Foundry` with the same scope idea.

## Hands-On Demo

- [Give an agent history and memory with context providers](./demo-context-providers.md)

## Key Topics

- [Context providers](https://learn.microsoft.com/en-us/agent-framework/agents/conversations/context-providers)
- [Storage](https://learn.microsoft.com/en-us/agent-framework/concepts/agents/conversations/storage)
- [Context provider integrations](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/context-providers/)
- [Microsoft Foundry context providers](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/context-providers/microsoft-foundry)
- [Create and use memory in Foundry Agent Service (preview)](https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/memory-usage)
