import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework.anthropic import AnthropicClient
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    agent = Agent(
        client=AnthropicClient(model=os.environ["ANTHROPIC_CHAT_MODEL"]),
        name="OutdoorGuide",
        instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
    )
    result = await agent.run("What should I pack for a day hike in the Alps?")
    print(result.text)


asyncio.run(main())
