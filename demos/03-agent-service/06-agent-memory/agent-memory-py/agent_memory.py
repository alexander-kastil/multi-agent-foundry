import os
import time
from datetime import timedelta

from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    MemorySearchOptions,
    MemorySearchPreviewTool,
    MemoryStoreDefaultDefinition,
    MemoryStoreDefaultOptions,
    PromptAgentDefinition,
)

load_dotenv(override=True)

STORE = "m3-memory-store"
AGENT = "m3-memory-agent"
MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()
memory_stores = project.beta.memory_stores


def message(role, content):
    return {"type": "message", "role": role, "content": content}


def show(memories):
    for entry in memories:
        print(f"  [{entry.memory_item.kind}] {entry.memory_item.content[:150]}")


print("== Step 1: create a memory store")
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
print(f"Created {STORE}")

print("== Step 2: write memories from a conversation")
conversation_items = [
    message("user", "I prefer dark roast coffee and I am allergic to peanuts."),
    message("user", "Please publish the release notes for version 2.4."),
    message("assistant", "I posted them straight to the wiki."),
    message(
        "user",
        "No, that is wrong. The process for publishing release notes is: first run the changelog script, "
        "then get approval from the release manager in the review channel, and only then publish to the wiki.",
    ),
    message("assistant", "Understood. I will run the changelog script, request approval from the release manager and publish only after approval."),
    message("user", "Right. Do the same for version 2.5."),
    message("assistant", "Running the changelog script for 2.5 now, then asking the release manager for approval before publishing."),
    message("user", "Good, that is how we always do it."),
]
result = memory_stores.begin_update_memories(name=STORE, scope="alice", items=conversation_items, update_delay=0).result()
for operation in result.memory_operations:
    print(f"  {operation.kind} [{operation.memory_item.kind}] {operation.memory_item.content[:150]}")

print("== Step 3: search static and contextual memories")
print("static memories for alice:")
show(memory_stores.search_memories(name=STORE, scope="alice").memories)
print("contextual memories for 'Publish the release notes for 2.6':")
show(
    memory_stores.search_memories(
        name=STORE,
        scope="alice",
        items=[message("user", "Publish the release notes for 2.6")],
        options=MemorySearchOptions(max_memories=5),
    ).memories
)
print(f"memories visible to bob: {len(memory_stores.search_memories(name=STORE, scope='bob', items=[message('user', 'coffee')]).memories)}")

print("== Step 4: attach the memory search tool to an agent")
agent = project.agents.create_version(
    agent_name=AGENT,
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="You are a helpful assistant. Use what you remember about the user and say when you remember nothing.",
        tools=[MemorySearchPreviewTool(memory_store_name=STORE, scope="{{$userId}}", update_delay=1)],
    ),
)
print(f"Created {agent.name}:{agent.version}")


def ask(user, conversation, text):
    return openai.responses.create(
        input=text,
        conversation=conversation.id,
        extra_headers={"x-memory-user-id": user},
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    )


first = openai.conversations.create()
print("carol, conversation 1: I prefer oat milk in my flat white")
print(f"agent: {ask('carol', first, 'I prefer oat milk in my flat white.').output_text}")
print("waiting for memories to be written")
time.sleep(65)

print("== Step 5: recall in a new conversation and prove scope isolation")
for user in ("carol", "dave"):
    fresh = openai.conversations.create()
    print(f"{user}, new conversation: Please order my usual coffee")
    print(f"agent: {ask(user, fresh, 'Please order my usual coffee.').output_text}")

print("== Step 6: remember and forget on command")
tools = [{"type": "memory_search_preview", "memory_store_name": STORE, "scope": "erin"}]
for text in ("Remember that my preferred seat is aisle.", "Forget my preferred seat."):
    response = openai.responses.create(model=MODEL, tools=tools, input=text)
    for item in response.output:
        if item.type == "memory_command_preview_call":
            print(f"{text} -> {item.arguments} ({item.status})")

print("== Cleanup")
for scope in ("alice", "bob", "carol", "dave", "erin"):
    memory_stores.delete_scope(name=STORE, scope=scope)
project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
memory_stores.delete(STORE)
print("Deleted scopes, agent version and memory store")
