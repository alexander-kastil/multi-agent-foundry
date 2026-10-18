import os
from urllib.parse import urlparse

from agent_framework.foundry import FoundryChatClient
from agent_framework.openai import OpenAIChatCompletionClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

_credential = DefaultAzureCredential()


def responses_agent(name: str, instructions: str, description: str | None = None, tools=None):
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions, description=description, tools=tools or [])


def chat_completions_agent(name: str, instructions: str, description: str | None = None, tools=None):
    host = urlparse(os.environ["PROJECT_ENDPOINT"]).netloc
    client = OpenAIChatCompletionClient(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        base_url=f"https://{host}/openai/v1/",
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions, description=description, tools=tools or [])
