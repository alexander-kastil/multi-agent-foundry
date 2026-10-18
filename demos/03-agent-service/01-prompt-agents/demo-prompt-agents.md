# Version and Converse with a Prompt Agent

A support agent's wording changes over time, and every change risks breaking whoever already depends on it. Chat history that lives in your own code is just as fragile. In this demo you publish two versions of one agent, call each by name and version, and keep a conversation on the server so the history survives a version change.

You finish with an `m3-support-agent` that has two immutable versions and a stored conversation that both versions answered.

## Prerequisites

- A Microsoft Foundry project with a chat model deployment (this guide uses `gpt-5.4`) and `az login` completed.
- The `Foundry User` role on the project.
- Python 3.12 or later for the Python project; the .NET 10 SDK for the C# project.

## Run it

```bash
cd demos/03-agent-service/01-prompt-agents/prompt-agents-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python prompt_agents.py
```

Edit `.env` first and set `PROJECT_ENDPOINT` to your project endpoint. The C# twin is in `prompt-agents-cs`: copy the same `.env`, then run `dotnet run`.

## Step 1: Create version 1

`create_version` registers the definition under the name `m3-support-agent` and returns the version number the service assigned.

```python
V1_INSTRUCTIONS = "You are a support agent for Contoso Outdoor. Answer in one short sentence."

def create_version(instructions):
    agent = project.agents.create_version(
        agent_name=AGENT_NAME,
        definition=PromptAgentDefinition(model=MODEL, instructions=instructions),
    )
    print(f"Created {agent.name}:{agent.version}")
    return agent

v1 = create_version(V1_INSTRUCTIONS)
```

Expected output:

```text
== Step 1: create version 1
Created m3-support-agent:1
```

## Step 2: Call the agent by name and version

The agent goes into `extra_body` as an `agent_reference`. The version pins the call to the definition you just created.

```python
def reference(version):
    return {"agent_reference": {"name": AGENT_NAME, "version": version, "type": "agent_reference"}}

response = openai.responses.create(
    input="My tent zipper is stuck. What should I do?",
    extra_body=reference(v1.version),
)
print(f"{AGENT_NAME}:{v1.version} -> {response.output_text}")
```

Expected output:

```text
== Step 2: single response, no conversation
m3-support-agent:1 -> Gently brush away any dirt, rub the zipper with candle wax or bar soap, and slowly work it back and forth without forcing it.
```

The wording differs on your run, but it stays one sentence because version 1 asks for one.

## Step 3: Hold a conversation on the server

The conversation starts with one seeded message. Each later response passes `conversation=conversation.id`, so the agent sees the earlier turns without you resending them.

```python
conversation = openai.conversations.create(
    items=[{"type": "message", "role": "user", "content": "I bought the Trailhead 2 tent last week."}]
)
for question in ["Which tent did I buy?", "How long ago was that?"]:
    turn = openai.responses.create(
        conversation=conversation.id,
        input=question,
        extra_body=reference(v1.version),
    )
    print(f"user: {question}\nagent: {turn.output_text}")
```

Expected output:

```text
== Step 3: conversation with history
Conversation: conv_00edb9b8f3d5205200NAkXf1lfvTovUiAXZGv6WYayyNWCCBIS
user: Which tent did I buy?
agent: You bought the Trailhead 2 tent.
user: How long ago was that?
agent: You bought it one week ago.
```

Neither question mentions the tent or the date. The answers come from the seeded item.

## Step 4: Read the conversation items

The conversation stores items, and every response appended its input and output to the list. Reading it back shows exactly what the next response will receive as context.

```python
for item in openai.conversations.items.list(conversation.id, order="asc"):
    text = item.content[0].text if item.type == "message" else ""
    print(f"{item.type:<8} {getattr(item, 'role', ''):<10} {text}")
```

Expected output:

```text
== Step 4: items in the conversation
message  user       I bought the Trailhead 2 tent last week.
message  user       Which tent did I buy?
message  assistant  You bought the Trailhead 2 tent.
message  user       How long ago was that?
message  assistant  You bought it one week ago.
```

## Step 5: Publish version 2 and compare

A second `create_version` call under the same name creates version 2. Version 1 stays untouched, so you can send the same question to both and compare.

```python
V2_INSTRUCTIONS = (
    "You are a support agent for Contoso Outdoor. "
    "Answer in two short sentences and end with a follow-up question."
)

v2 = create_version(V2_INSTRUCTIONS)
for version in (v1.version, v2.version):
    answer = openai.responses.create(
        input="My tent zipper is stuck. What should I do?",
        extra_body=reference(version),
    )
    print(f"{AGENT_NAME}:{version} -> {answer.output_text}")
```

Expected output:

```text
== Step 5: create version 2 and compare
Created m3-support-agent:2
m3-support-agent:1 -> Gently back the zipper up, remove any fabric caught, clean the teeth, and rub on a little zipper lubricant or candle wax.
m3-support-agent:2 -> Gently brush out any dirt or fabric caught in the zipper, then apply a small amount of zipper lubricant, wax, or soap and work it slowly without forcing it. If the teeth are misaligned ... would you like troubleshooting for a coil zipper?
```

## Step 6: Continue the same conversation on version 2

History belongs to the conversation, so a newer version can pick up where version 1 stopped. The script then lists both versions and removes everything it created.

```python
turn = openai.responses.create(
    conversation=conversation.id,
    input="Summarize what we discussed.",
    extra_body=reference(v2.version),
)
print(f"{AGENT_NAME}:{v2.version} -> {turn.output_text}")

for version in project.agents.list_versions(AGENT_NAME):
    print(f"{version.name}:{version.version} model={version.definition.model}")
```

Expected output:

```text
== Step 6: same conversation continues on version 2
m3-support-agent:2 -> You said you bought the Trailhead 2 tent last week, and I confirmed it was the Trailhead 2. Would you like help with anything else about your tent?
== Versions
m3-support-agent:2 model=gpt-5.4
m3-support-agent:1 model=gpt-5.4
```

Version 2 answers with two sentences and a closing question, and it still knows about the tent.

## Verify

The versions list shows `m3-support-agent:1` and `m3-support-agent:2`, both on `gpt-5.4`. Step 6 summarizes a purchase that only the seeded item and version 1 ever saw, which proves the history lives in the conversation and not in an agent version.

## Run the C# version

```bash
cd demos/03-agent-service/01-prompt-agents/prompt-agents-cs
cp .env.example .env
dotnet run
```

The C# program creates the same two versions and shows the same pinning and conversation hand-over. `GetProjectResponsesClientForAgent(agentReference, conversation)` binds the agent version and the conversation for every call on that client.

```csharp
ProjectConversation conversation = await project.ProjectOpenAIClient
    .GetProjectConversationsClient().CreateProjectConversationAsync();
ProjectResponsesClient chat = project.ProjectOpenAIClient
    .GetProjectResponsesClientForAgent(new AgentReference(v1.Name, v1.Version), conversation);
ResponseResult turn = await chat.CreateResponseAsync("Which tent did I buy?");
```

Expected output:

```text
== Step 4: create version 2 and continue the same conversation
Created m3-support-agent:2
m3-support-agent:2 -> You said you bought the Trailhead 2 tent last week, and I confirmed that the tent you bought was the Trailhead 2. Is there anything else you'd like help with?
```

## Cleanup

The Python script deletes the conversation and both versions at the end; the C# program deletes both versions. To remove a version by hand:

```python
project.agents.delete_version(agent_name="m3-support-agent", agent_version="1")
```
