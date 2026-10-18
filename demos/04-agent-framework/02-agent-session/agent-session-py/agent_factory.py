import os

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)


def build_agent() -> Agent:
    return Agent(
        client=FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=DefaultAzureCredential(),
        ),
        name="TripPlanner",
        instructions="You are a trip planner for Contoso Outdoor. Answer in two sentences and use what the user told you earlier.",
    )
