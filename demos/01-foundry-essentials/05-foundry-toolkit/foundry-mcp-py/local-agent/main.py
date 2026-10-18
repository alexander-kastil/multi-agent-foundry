import os
from typing import Annotated

from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from agent_framework_foundry_hosting import ResponsesHostServer
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from pydantic import Field

load_dotenv(override=True)


@tool(approval_mode="never_require")
def get_weather(
    city: Annotated[str, Field(description="The city to look up.")],
) -> str:
    """Get the current weather for a city."""
    return f"The weather in {city} is sunny with a high of 21 degrees Celsius."


def main() -> None:
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    agent = Agent(
        client=client,
        instructions="You are a weather assistant. Always use the get_weather tool and keep answers to one sentence.",
        tools=[get_weather],
        default_options={"store": False},
    )
    ResponsesHostServer(agent).run()


if __name__ == "__main__":
    main()
