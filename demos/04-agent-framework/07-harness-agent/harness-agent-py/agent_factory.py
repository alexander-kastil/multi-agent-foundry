import os

from agent_framework import Agent, create_harness_agent
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

from trail_tools import get_stage_status, reserve_hut

load_dotenv(override=True)

INSTRUCTIONS = (
    "You plan multi-day treks for Contoso Outdoor. Check every stage with get_stage_status before you reserve "
    "huts with reserve_hut. Keep every answer under five sentences and avoid dashes."
)


def build_client() -> FoundryChatClient:
    return FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )


def build_harness(**overrides) -> Agent:
    options = dict(
        client=build_client(),
        name="TrekPlanner",
        agent_instructions=INSTRUCTIONS,
        tools=[get_stage_status, reserve_hut],
        disable_web_search=True,
        disable_file_memory=True,
        default_options={"store": False},
    )
    options.update(overrides)
    return create_harness_agent(**options)
