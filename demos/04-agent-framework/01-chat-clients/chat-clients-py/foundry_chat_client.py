import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    agent = Agent(
        client=client,
        name="OutdoorGuide",
        instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
    )

    result = await agent.run("What should I pack for a day hike in the Alps?")
    print(result.text)

    print()
    stream = agent.run("Name three Austrian mountain ranges.", stream=True)
    printed = ""
    async for update in stream:
        if update.text and update.text != printed:
            print(update.text, end="", flush=True)
            printed += update.text
    print()
    final = await stream.get_final_response()
    print(f"\nfinal response length: {len(final.text)} characters")


asyncio.run(main())
