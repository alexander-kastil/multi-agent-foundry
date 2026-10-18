import asyncio
import os
import sys

import httpx
from a2a.client import A2ACardResolver
from agent_framework.a2a import A2AAgent
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

TITLE_URL = f"http://localhost:{os.getenv('TITLE_PORT', '9101')}"


async def main() -> None:
    async with httpx.AsyncClient(timeout=60.0) as http_client:
        card = await A2ACardResolver(httpx_client=http_client, base_url=TITLE_URL).get_agent_card()
    print(f"card: {card.name} skills={[skill.id for skill in card.skills]}")

    async with A2AAgent(agent_card=card) as agent:
        response = await agent.run("Hosted agents without the plumbing")
        print(f"title: {response.messages[0].text}")


asyncio.run(main())
