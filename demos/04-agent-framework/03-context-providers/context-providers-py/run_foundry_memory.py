import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient, FoundryMemoryProvider
from azure.ai.projects.aio import AIProjectClient
from azure.identity.aio import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

SCOPE = "user-alex"


async def main() -> None:
    async with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project,
    ):
        client = FoundryChatClient(project_client=project, model=os.environ["MODEL_DEPLOYMENT_NAME"])
        memory = FoundryMemoryProvider(
            project_client=project,
            memory_store_name=os.environ["MEMORY_STORE_NAME"],
            scope=SCOPE,
            update_delay=0,
        )
        agent = Agent(
            client=client,
            name="TrailGuide",
            instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences and use remembered facts.",
            context_providers=[memory],
            default_options={"store": False},
        )

        first = agent.create_session()
        await agent.run("I hike with a dog called Bruno, and I dislike steep climbs.", session=first)
        await asyncio.sleep(10)

        second = agent.create_session()
        result = await agent.run("Which kind of trail suits me?", session=second)
        print(result.text)

        found = await project.beta.memory_stores.search_memories(
            name=os.environ["MEMORY_STORE_NAME"], scope=SCOPE
        )
        for item in found.memories:
            print(f"memory: {item.memory_item.content}")


asyncio.run(main())
