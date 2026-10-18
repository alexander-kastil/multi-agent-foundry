# Swap Chat Clients Under One Agent

Agent code that hardcodes one model SDK has to be rewritten the day the model, the cloud or the owner of the agent changes. In this demo you run the same trail-guide agent on four different clients, connect to an agent that Foundry owns, and read the one place where Claude would plug in. You finish with a project that proves the agent code stays put while the client moves.

What you practice here: choosing the chat client that matches who owns the agent definition.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later and the Azure CLI signed in with `az login` on an account that can call the Foundry project.
- A Foundry project with a chat deployment (the demo uses `gpt-5.4`) and the Azure OpenAI endpoint of the same resource.
- For the C# step: .NET 10 SDK.

## Run it

```powershell
cd chat-clients-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME` and `AZURE_OPENAI_ENDPOINT` in `.env`.

```powershell
python foundry_chat_client.py
```

The finished project is `chat-clients-py/`; the C# version sits beside it in `chat-clients-cs/`.

## Step 1: Run an agent on a Foundry deployment

`FoundryChatClient` points the agent at a model deployed in your Foundry project. The agent owns its instructions, so changing the behavior means changing this file.

```python
client = FoundryChatClient(
    project_endpoint=os.environ["PROJECT_ENDPOINT"],
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    credential=DefaultAzureCredential(),
)
agent = Agent(
    client=client,
    name="OutdoorGuide",
    instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
)

result = await agent.run("What should I pack for a day hike in the Alps?")
print(result.text)
```

The second half of the script streams with `run(stream=True)`. The stream ends with one update that repeats the whole text, so the script prints only updates that differ from what it already printed, then reads the combined result with `get_final_response()`.

```python
stream = agent.run("Name three Austrian mountain ranges.", stream=True)
printed = ""
async for update in stream:
    if update.text and update.text != printed:
        print(update.text, end="", flush=True)
        printed += update.text
final = await stream.get_final_response()
```

Expected output:

```text
For a day hike in the Alps, pack layers for quickly changing weather: a moisture-wicking base layer, warm midlayer, waterproof shell, sturdy hiking boots, hat, gloves, sunscreen, sunglasses, and a small first-aid kit. Bring plenty of water, high-energy snacks, a map or GPS, fully charged phone, and trekking poles if the trail is steep or rocky.

The Alps dominate Austria, and three notable ranges within them are the Tyrol Alps, the Hohe Tauern, and the Dachstein Mountains. If you'd like, I can also name famous peaks or hiking areas in each.

final response length: 198 characters
```

The wording is model-dependent; the structure is not: one blocking answer, one streamed answer, one final length.

## Step 2: Swap in the OpenAI clients

The agent, its name and its instructions stay identical; only the client object changes. `OpenAIChatClient` speaks the Responses API and `OpenAIChatCompletionClient` speaks Chat Completions, both against the Azure OpenAI endpoint of the same resource.

```python
for client in (
    OpenAIChatClient(model=model, azure_endpoint=azure_endpoint, credential=credential),
    OpenAIChatCompletionClient(model=model, azure_endpoint=azure_endpoint, credential=credential),
):
    agent = Agent(client=client, name="OutdoorGuide", instructions=INSTRUCTIONS)
    result = await agent.run(QUESTION)
    print(f"[{type(client).__name__}] {result.text}\n")
```

```powershell
python openai_clients.py
```

Expected output:

```text
[OpenAIChatClient] Pack layered clothing, a waterproof shell, sturdy hiking boots, map/GPS, plenty of water, high-energy snacks, sun protection, a hat and gloves, a first-aid kit, and a headlamp. In the Alps, weather changes fast, so add trekking poles, an emergency blanket, and extra warm layers even for a short day hike.

[OpenAIChatCompletionClient] Pack layered clothing, a waterproof shell, sturdy hiking boots, map/GPS, sun protection, first-aid kit, plenty of water, high-energy snacks, and a headlamp. In the Alps, weather changes fast, so also bring warm gloves/hat and check the forecast and trail conditions before you go.
```

Both lines come from the same deployment; only the wire protocol differs. Pick the Responses client when you need hosted tools, and Chat Completions when you target an OpenAI-compatible endpoint.

## Step 3: Create a prompt agent that Foundry owns

`FoundryAgent` needs a server-side agent to connect to. This script registers a Prompt Agent through `azure-ai-projects` with its own instructions, and prints the version Foundry assigned.

```python
version = project.agents.create_version(
    agent_name=os.environ["FOUNDRY_AGENT_NAME"],
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions="You are the returns desk of Contoso Outdoor. Answer in two sentences and always quote the 30 day return window.",
    ),
)
print(f"created {version.name} version {version.version}")
```

```powershell
python create_prompt_agent.py
```

Expected output:

```text
created m4-support-agent version 1
```

## Step 4: Connect with FoundryAgent and see who owns the instructions

The script never states instructions for this agent; the stored definition supplies them. It then tries to override them for one run to show where the boundary sits.

```python
agent = FoundryAgent(
    project_endpoint=os.environ["PROJECT_ENDPOINT"],
    agent_name=os.environ["FOUNDRY_AGENT_NAME"],
    agent_version="1",
    credential=DefaultAzureCredential(),
)

result = await agent.run("How do I return a tent?")
print(result.text)

try:
    await agent.run(
        "How do I return a tent?",
        options={"instructions": "Answer only in French."},
    )
except Exception as error:
    message = error.__cause__.body["message"]
    print(f"per-run instructions rejected: {message}")
```

```powershell
python foundry_agent.py
```

Expected output:

```text
To return a tent, start a return through your Contoso Outdoor order history or bring it with your receipt/order number to a Contoso Outdoor store, and it must be in returnable condition within the "30 day return window." If you want, I can also help with mail-in return steps, refunds, or exchanges within the "30 day return window."
per-run instructions rejected: Not allowed when agent is specified. [Request ID: fd31f8dcb7f0c29d4fb39d4f6ab4e7b0]
```

The quoted 30 day window proves the answer used the stored instructions, not anything in the script. The Learn page for `FoundryAgent` says per-run instructions are ignored; with this service version the request is rejected with a 400 instead. Either way you cannot change a Foundry-owned agent from the client.

## Step 5: Plug in Claude

Claude uses the same `Agent`, with `AnthropicClient` from the beta `agent-framework-anthropic` package. The file `anthropic_client.py` is the whole change.

```python
agent = Agent(
    client=AnthropicClient(model=os.environ["ANTHROPIC_CHAT_MODEL"]),
    name="OutdoorGuide",
    instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
)
```

```powershell
pip install -r requirements-anthropic.txt
python anthropic_client.py
```

This step needs `ANTHROPIC_API_KEY` in `.env`, or a Claude deployment on your Foundry resource with `AnthropicFoundryClient`. The Foundry project used to author this demo has no Claude deployment, so this step was not run here. With a valid key the script prints a two-sentence packing answer, exactly like Step 1.

## Step 6: Repeat the swap in C#

The C# project builds the same three agents with `AsAIAgent`. The first uses the project client, the second wraps an Azure OpenAI `ChatClient` through `AsIChatClient()`, and the third references the Prompt Agent from Step 3.

```csharp
AIAgent foundryChatAgent = projectClient.AsAIAgent(
    model: model,
    instructions: instructions,
    name: "OutdoorGuide");

AIAgent chatCompletionAgent = azureOpenAI
    .GetChatClient(model)
    .AsIChatClient()
    .AsAIAgent(instructions: instructions, name: "OutdoorGuide");

FoundryAgent promptAgent = projectClient.AsAIAgent(new AgentReference(agentName, "1"));
```

```powershell
cd ../chat-clients-cs
Copy-Item .env.example .env
dotnet run
```

Expected output:

```text
[FoundryChatClient] For a day hike in the Alps, pack layered clothing, a waterproof jacket, sturdy hiking boots, plenty of water, high-energy snacks, a map or GPS, a first-aid kit, sun protection, and extra warm gear since mountain weather can change quickly. ...

Three Austrian mountain ranges are the Alps, the Hohe Tauern, and the Karawanks. ...

[ChatClient] For a day hike in the Alps, pack layered clothing (including a waterproof shell), sturdy hiking boots, plenty of water, ...

[FoundryAgent] You can return a tent by starting a return through your Contoso Outdoor order history or by bringing it, with proof of purchase, to a Contoso Outdoor store. Our return window is "30 days" from the purchase date.
```

The project pins stable `Microsoft.Agents.AI.Foundry` 1.5.0 with `Microsoft.Agents.AI` 1.25.0. The 1.25.0 preview of the Foundry package depends on `Azure.AI.Projects` 3.0.0-beta.3, and with it the Prompt Agent created from Python in Step 3 was reported as not found.

## Verify

Step 2 prints two labelled answers, Step 6 prints three, and the Prompt Agent answers quote a 30 day return window you never typed into a script. List the agent in the project to confirm Foundry holds it.

```powershell
python -c "import os; from dotenv import load_dotenv; from azure.ai.projects import AIProjectClient; from azure.identity import DefaultAzureCredential; load_dotenv(override=True); p = AIProjectClient(endpoint=os.environ['PROJECT_ENDPOINT'], credential=DefaultAzureCredential()); print([v.version for v in p.agents.list_versions(os.environ['FOUNDRY_AGENT_NAME'])])"
```

Expected output:

```text
['1']
```

## Cleanup

```powershell
cd ../chat-clients-py
python delete_prompt_agent.py
```

Expected output:

```text
deleted m4-support-agent
```
