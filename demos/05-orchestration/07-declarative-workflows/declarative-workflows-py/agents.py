import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

_credential = DefaultAzureCredential()


def make_agent(name: str, instructions: str):
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions)
