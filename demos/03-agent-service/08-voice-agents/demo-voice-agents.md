# Talk to Two Kinds of Voice Agent

A phone-style conversation needs speech in, speech out and a reply that arrives while the person is still waiting. Building that from separate speech-to-text, chat and text-to-speech calls is slow and fragile. In this demo you build the same support voice twice, once as a voice-based prompt agent and once as a text agent behind the Voice Live API, and you talk to both without a microphone.

You finish with two agents that answer in spoken audio, a WAV file of each reply and a stored voice conversation that you read back as text.

## Prerequisites

- A Microsoft Foundry project in a region that supports voice agents, with `az login` completed and the Foundry User role on the project.
- A `gpt-5.4` deployment for the text agent. The voice-based prompt agent uses the managed `gpt-realtime` model, so it needs no deployment.
- Access to the voice-based prompt agent preview in your subscription and region.
- The Voice Live endpoint of your Foundry resource (for example `https://<resource>.cognitiveservices.azure.com/`).
- Python 3.12 or later; the .NET 10 SDK for the C# project.
- No microphone or speaker is needed. Play the saved WAV files to listen.

## Run it

```bash
cd demos/03-agent-service/08-voice-agents/voice-agents-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python voice_prompt_agent.py
.venv/Scripts/python voice_live_agent.py
```

Set the five values in `.env` first. `PROJECT_NAME` is the last segment of the project endpoint.

## Step 1: Create a voice-based prompt agent

A voice agent is an agent whose kind is `voice`. The definition holds a managed real-time model, the instructions, an Azure standard voice and `store=True`, which keeps the transcript and audio for read-back. Voice definitions go through the standard `create_version` call, which needs `allow_preview=True` on the client.

```python
definition = VoiceAgentDefinition(
    model_type=VoiceModelType.MANAGED,
    model=os.environ["VOICE_MODEL"],
    instructions=(
        "You are a voice assistant for Contoso Outdoor camping gear. "
        "Speak in short sentences, keep replies to one or two sentences and ask one question at a time."
    ),
    audio=VoiceAgentAudioConfig(
        output=VoiceAgentAudioOutputConfig(voice="en-US-AvaNeural", voice_type=VoiceType.AZURE_STANDARD),
    ),
    output_modalities=[VoiceOutputModality.AUDIO],
    store=True,
)
created = project.agents.create_version(agent_name=AGENT, definition=definition)
```

Expected output:

```text
== Step 1: create a voice-based prompt agent
Created m3-voice-agent:1 (kind voice)
```

The instructions are written for listening: short sentences and one question at a time.

## Step 2: Talk to the agent over a realtime session

The script opens a realtime connection, sends each user turn as a text message item and asks for a response. The reply arrives as audio deltas plus a transcript, and each reply is saved as a PCM16, 24 kHz, mono WAV file.

```python
with project.beta.voice_agents.realtime.connect(agent_name=AGENT) as conn:
    conn.conversation.item.create(
        item=RealtimeConversationItemMessageUser(
            type=RealtimeConversationItemType.MESSAGE,
            content=[RealtimeConversationItemMessageUserContent(type="input_text", text=question)],
        )
    )
    conn.response.create()
    session_conversation_id, audio = receive_response()
```

Expected output (the wording varies):

```text
== Step 2: talk to the agent over a realtime session
user: Hi. In one sentence, what can you help me with?
agent: I can help you choose and learn about camping gear, answer outdoor adventure questions, and give tips for your trips ...
saved reply-1.wav (17 audio chunks, PCM16 24 kHz mono)
user: My tent zipper is stuck. What should I try first?
agent: First, try gently cleaning the zipper teeth with a cloth and applying a little bit of lubricant, like candle wax or soap, to help it slide. Did that help at all?
saved reply-2.wav (22 audio chunks, PCM16 24 kHz mono)
```

The agent owns the model, voice and turn detection on the server, so the client sends no session configuration. Play `reply-2.wav` to hear the answer. The second reply ends with a single question, as the instructions asked.

## Step 3: Read the stored conversation

Because the agent stores conversations, the session id from `session.created` doubles as a conversation id. The transcript comes back in order through the voice conversations API.

```python
conversations = project.beta.voice_agents.conversations
conversation = conversations.get(AGENT, conversation_id)
for item in conversations.list_items(AGENT, conversation_id, order="asc"):
    ...
```

Expected output:

```text
== Step 3: read the stored conversation
conversation conv_054c11eb75c351e5003vn7Q8qMdmLxfd5LrkXGDVv3BgstqoWO: status=VoiceConversationStatus.COMPLETED
  user: Hi. In one sentence, what can you help me with?
  assistant: I can help you choose and learn about camping gear, answer outdoor adventure questions, and give tips for your trips ...
  user: My tent zipper is stuck. What should I try first?
  assistant: First, try gently cleaning the zipper teeth with a cloth and applying a little bit of lubricant, like candle wax or soap, to help it slide. Did that help at all?
Deleted conversation and agent m3-voice-agent
```

The script then deletes the conversation and the agent.

## Step 4: Create a text agent with Voice Live settings

The second path keeps an ordinary text agent. The Voice Live settings (voice, turn detection, noise suppression, echo cancellation) are stored in the agent's metadata under `microsoft.voice-live.configuration`, split into 512-character chunks.

```python
agent = project.agents.create_version(
    agent_name=AGENT,
    definition=PromptAgentDefinition(model=os.environ["MODEL_DEPLOYMENT_NAME"], instructions="..."),
    metadata=chunk_config(json.dumps(VOICE_LIVE_CONFIG)),
)
```

Expected output:

```text
== Step 4: create a text agent with Voice Live settings
Created m3-voice-live-agent:1 with Voice Live settings in metadata
```

## Step 5: Talk to it through the Voice Live API

The client connects with `azure.ai.voicelive.aio.connect`, naming the agent, its version and the project. Agent mode needs Microsoft Entra ID, so the script passes an async `DefaultAzureCredential` and never a key. The first event is `session.updated`, which also reports which voice the agent supplied.

```python
async with connect(
    endpoint=os.environ["VOICELIVE_ENDPOINT"],
    credential=credential,
    agent_name=agent.name,
    agent_version=agent.version,
    project_name=os.environ["PROJECT_NAME"],
) as connection:
    await connection.session.update(
        session=RequestSession(
            modalities=[Modality.TEXT, Modality.AUDIO],
            input_audio_format=InputAudioFormat.PCM16,
            output_audio_format=OutputAudioFormat.PCM16,
        )
    )
```

Expected output:

```text
== Step 5: talk to it through the Voice Live API
session ready: agent m3-voice-live-agent, voice en-US-Ava:DragonHDLatestNeural (azure-standard)
user: Hello, I want to ask about the Summit 4S tent.
agent: Absolutely ... what would you like to know about the Summit 4S? It's a four-season tent rated to minus 25 degrees Celsius.
user: Is it suitable for winter camping?
agent: Yes ... the Summit 4S is designed for winter camping. It's a four-season tent rated to minus 25 degrees Celsius.
saved voice-live-reply.wav (34 audio chunks, PCM16 24 kHz mono)
Deleted agent m3-voice-live-agent
```

The voice name in the first line comes from the metadata you stored in Step 4, not from the session code. The facts in the answer come from the agent's instructions, which proves the Voice Live service called your agent for every turn.

## Step 6: Do the same from C#

`Azure.AI.VoiceLive` 1.2.0 offers the same flow. `AgentSessionConfig` carries the agent name, project and version, and `SessionTarget.FromAgent` starts the session. The program also creates the agent with the chunked metadata and deletes it at the end.

```bash
cd demos/03-agent-service/08-voice-agents/voice-agents-cs
cp .env.example .env
dotnet run
```

```csharp
AgentSessionConfig agentConfig = new(agent.Name, Environment.GetEnvironmentVariable("PROJECT_NAME")!) { AgentVersion = agent.Version };
using VoiceLiveSession session = await client.StartSessionAsync(SessionTarget.FromAgent(agentConfig));
```

Expected output:

```text
== Create a text agent with Voice Live settings
Created m3-voice-live-agent-cs:1
== Talk to it through the Voice Live API
session ready
user: Hello, I want to ask about the Summit 4S tent.
agent: Sure ... what would you like to know about the Summit 4S?
user: Is it suitable for winter camping?
agent: Yes ... the Summit 4S is built for four-season use and is rated down to minus 25 degrees Celsius. It's suitable for winter camping in very cold conditions.
saved voice-live-reply.wav (624000 bytes, PCM16 24 kHz mono)
Deleted agent m3-voice-live-agent-cs
```

## Verify

Both agents returned spoken audio (non-empty WAV files), the Voice Live session reported the voice you stored in metadata and the stored voice conversation reads back with all four turns. Both agents are gone from the project afterwards.

## Cleanup

Each script deletes the agents and conversation it creates. The WAV files stay in the project folder and are ignored by git; delete them when you are done.

```bash
rm -f reply-1.wav reply-2.wav voice-live-reply.wav
```

For a hands-free conversation with a microphone, use the live-audio samples linked in the Voice Live and voice agent quickstarts. They stream microphone audio into the same session and use server-side turn detection to decide when you stop speaking.
