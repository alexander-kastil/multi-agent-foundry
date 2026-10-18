import asyncio
import json
import os
import wave

from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.identity.aio import DefaultAzureCredential as AsyncDefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.ai.voicelive.aio import connect
from azure.ai.voicelive.models import (
    InputAudioFormat,
    InputTextContentPart,
    MessageItem,
    Modality,
    OutputAudioFormat,
    RequestSession,
    ServerEventType,
)

load_dotenv(override=True)

AGENT = "m3-voice-live-agent"
QUESTIONS = [
    "Hello, I want to ask about the Summit 4S tent.",
    "Is it suitable for winter camping?",
]

VOICE_LIVE_CONFIG = {
    "session": {
        "voice": {"name": "en-US-Ava:DragonHDLatestNeural", "type": "azure-standard", "temperature": 0.8},
        "input_audio_transcription": {"model": "azure-speech"},
        "turn_detection": {
            "type": "azure_semantic_vad",
            "end_of_utterance_detection": {"model": "semantic_detection_v1_multilingual"},
        },
        "input_audio_noise_reduction": {"type": "azure_deep_noise_suppression"},
        "input_audio_echo_cancellation": {"type": "server_echo_cancellation"},
    }
}


def chunk_config(config_json, limit=512):
    metadata = {"microsoft.voice-live.configuration": config_json[:limit]}
    remaining = config_json[limit:]
    number = 1
    while remaining:
        metadata[f"microsoft.voice-live.configuration.{number}"] = remaining[:limit]
        remaining = remaining[limit:]
        number += 1
    return metadata


def create_agent():
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project,
    ):
        agent = project.agents.create_version(
            agent_name=AGENT,
            definition=PromptAgentDefinition(
                model=os.environ["MODEL_DEPLOYMENT_NAME"],
                instructions=(
                    "You are a voice assistant for Contoso Outdoor. Reply in one or two short spoken sentences. "
                    "The Summit 4S is a four-season tent rated to minus 25 degrees Celsius."
                ),
            ),
            metadata=chunk_config(json.dumps(VOICE_LIVE_CONFIG)),
        )
        print(f"Created {agent.name}:{agent.version} with Voice Live settings in metadata")
        return agent


async def talk(agent):
    audio = []
    async with AsyncDefaultAzureCredential() as credential:
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
            pending = list(QUESTIONS)
            async for event in connection:
                if event.type == ServerEventType.SESSION_UPDATED:
                    voice = event.session.voice
                    print(f"session ready: agent {event.session.agent.name}, voice {voice['name']} ({voice['type']})")
                    question = pending.pop(0)
                    print(f"user: {question}")
                    await connection.conversation.item.create(
                        item=MessageItem(role="user", content=[InputTextContentPart(text=question)])
                    )
                    await connection.response.create()
                elif event.type == ServerEventType.RESPONSE_AUDIO_DELTA:
                    audio.append(event.delta)
                elif event.type == ServerEventType.RESPONSE_AUDIO_TRANSCRIPT_DONE:
                    print(f"agent: {event.transcript}")
                elif event.type == ServerEventType.ERROR:
                    raise RuntimeError(event.error.message)
                elif event.type == ServerEventType.RESPONSE_DONE:
                    if not pending:
                        break
                    question = pending.pop(0)
                    print(f"user: {question}")
                    await connection.conversation.item.create(
                        item=MessageItem(role="user", content=[InputTextContentPart(text=question)])
                    )
                    await connection.response.create()
    with wave.open("voice-live-reply.wav", "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(24000)
        wav.writeframes(b"".join(audio))
    print(f"saved voice-live-reply.wav ({len(audio)} audio chunks, PCM16 24 kHz mono)")


def delete_agent(agent):
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project,
    ):
        project.agents.delete(agent_name=agent.name)
        print(f"Deleted agent {agent.name}")


print("== Step 4: create a text agent with Voice Live settings")
agent = create_agent()
print("== Step 5: talk to it through the Voice Live API")
try:
    asyncio.run(talk(agent))
finally:
    delete_agent(agent)
