import os
import wave

from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    RealtimeConversationItemMessageUser,
    RealtimeConversationItemMessageUserContent,
    RealtimeConversationItemType,
    RealtimeServerEventError,
    RealtimeServerEventResponseAudioDelta,
    RealtimeServerEventResponseAudioTranscriptDone,
    RealtimeServerEventResponseDone,
    RealtimeServerEventSessionCreated,
    VoiceAgentAudioConfig,
    VoiceAgentAudioOutputConfig,
    VoiceAgentDefinition,
    VoiceModelType,
    VoiceOutputModality,
    VoiceType,
)

load_dotenv(override=True)

AGENT = "m3-voice-agent"
QUESTIONS = [
    "Hi. In one sentence, what can you help me with?",
    "My tent zipper is stuck. What should I try first?",
]

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

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential, allow_preview=True) as project,
):
    print("== Step 1: create a voice-based prompt agent")
    created = project.agents.create_version(agent_name=AGENT, definition=definition)
    print(f"Created {AGENT}:{created.version} (kind {created.definition.kind})")

    print("== Step 2: talk to the agent over a realtime session")
    conversation_id = None
    with project.beta.voice_agents.realtime.connect(agent_name=AGENT) as conn:

        def receive_response():
            chunks = []
            session_conversation_id = None
            while True:
                event = conn.recv(timeout=45)
                if isinstance(event, RealtimeServerEventSessionCreated):
                    session_conversation_id = event.conversation_id
                elif isinstance(event, RealtimeServerEventResponseAudioDelta):
                    chunks.append(event.delta)
                elif isinstance(event, RealtimeServerEventResponseAudioTranscriptDone):
                    print(f"agent: {event.transcript}")
                elif isinstance(event, RealtimeServerEventError):
                    raise RuntimeError(event.error.message)
                elif isinstance(event, RealtimeServerEventResponseDone):
                    if event.response.status != "completed":
                        raise RuntimeError(f"Response status: {event.response.status}")
                    return session_conversation_id, chunks

        for number, question in enumerate(QUESTIONS, start=1):
            print(f"user: {question}")
            conn.conversation.item.create(
                item=RealtimeConversationItemMessageUser(
                    type=RealtimeConversationItemType.MESSAGE,
                    content=[RealtimeConversationItemMessageUserContent(type="input_text", text=question)],
                )
            )
            conn.response.create()
            session_conversation_id, audio = receive_response()
            conversation_id = session_conversation_id or conversation_id
            with wave.open(f"reply-{number}.wav", "wb") as wav:
                wav.setnchannels(1)
                wav.setsampwidth(2)
                wav.setframerate(24000)
                wav.writeframes(b"".join(audio))
            print(f"saved reply-{number}.wav ({len(audio)} audio chunks, PCM16 24 kHz mono)")

    print("== Step 3: read the stored conversation")
    conversations = project.beta.voice_agents.conversations
    conversation = conversations.get(AGENT, conversation_id)
    print(f"conversation {conversation.id}: status={conversation.status}")
    for item in conversations.list_items(AGENT, conversation_id, order="asc"):
        parts = [(part.get("transcript") or part.get("text") or "").strip() for part in (item.get("content") or [])]
        text = " ".join(part for part in parts if part)
        if text:
            print(f"  {item.get('role')}: {text}")

    print("== Cleanup")
    conversations.delete(AGENT, conversation_id)
    project.agents.delete(agent_name=AGENT)
    print(f"Deleted conversation and agent {AGENT}")
