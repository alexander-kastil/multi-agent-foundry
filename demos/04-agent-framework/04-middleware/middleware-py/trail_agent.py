import os
from collections.abc import Sequence
from typing import Annotated

from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from pydantic import Field

load_dotenv(override=True)

TRAILS = {
    "tauern high route": "open, snow above 2400 m",
    "wachau valley loop": "open, dry",
    "dachstein traverse": "closed, rockfall warning",
}


@tool(approval_mode="never_require")
def get_trail_status(
    trail: Annotated[str, Field(description="Trail name, for example 'Wachau valley loop'.")],
) -> str:
    """Return the current status of a hiking trail."""
    return TRAILS.get(trail.lower(), "unknown trail")


def build_agent(middleware: Sequence | None = None) -> Agent:
    return Agent(
        client=FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=DefaultAzureCredential(),
        ),
        name="TrailGuide",
        instructions="You are a trail guide for Contoso Outdoor. Use get_trail_status before you recommend a trail. Answer in two sentences without dashes.",
        tools=get_trail_status,
        middleware=list(middleware or []),
    )
