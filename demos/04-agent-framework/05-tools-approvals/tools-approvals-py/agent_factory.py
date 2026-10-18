import os

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)


def build_client() -> FoundryChatClient:
    return FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )


def build_agent(tools, instructions: str = "You are a trail guide for Contoso Outdoor. Answer in two sentences without dashes.") -> Agent:
    return Agent(client=build_client(), name="TrailGuide", instructions=instructions, tools=tools)


def tool_calls(result) -> list[str]:
    return [
        f"{content.name}({content.arguments})"
        for message in result.messages
        for content in message.contents
        if content.type == "function_call"
    ]
