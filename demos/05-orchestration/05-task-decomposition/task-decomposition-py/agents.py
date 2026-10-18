import json
import os

from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

_credential = DefaultAzureCredential()


def make_agent(name: str, instructions: str, handoff: bool = False):
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions, require_per_service_call_history_persistence=handoff)


def parse_json(text: str) -> dict:
    cleaned = text.strip()
    if cleaned.startswith("```"):
        cleaned = cleaned.split("\n", 1)[1].rsplit("```", 1)[0]
    return json.loads(cleaned)


async def ask_json(agent, prompt: str) -> dict:
    return parse_json((await agent.run(prompt)).text)
