import os

from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

_credential = DefaultAzureCredential()


def make_agent(name: str, instructions: str, tools=None, description: str | None = None, handoff: bool = False):
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=_credential,
    )
    return client.as_agent(
        name=name,
        instructions=instructions,
        tools=tools or [],
        description=description,
        require_per_service_call_history_persistence=handoff,
    )
