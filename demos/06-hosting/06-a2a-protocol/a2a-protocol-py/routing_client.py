import asyncio
import os
import sys

import httpx
from a2a.client import A2ACardResolver
from agent_framework import Agent
from agent_framework.a2a import A2AAgent
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

REMOTES = [
    f"http://localhost:{os.getenv('TITLE_PORT', '9101')}",
    f"http://localhost:{os.getenv('OUTLINE_PORT', '9102')}",
]


async def main() -> None:
    remotes = []
    async with httpx.AsyncClient(timeout=60.0) as http_client:
        for url in REMOTES:
            card = await A2ACardResolver(httpx_client=http_client, base_url=url).get_agent_card()
            remotes.append(A2AAgent(agent_card=card))

    for remote in remotes:
        print(f"found remote agent: {remote.name}")

    router = Agent(
        client=FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=DefaultAzureCredential(),
        ),
        name="m6h2-routing-agent",
        instructions=(
            "You route requests to remote agents. Ask the title agent for a title first, "
            "then pass that title to the outline agent, and return both."
        ),
        tools=[remote.as_tool(name=remote.name.replace("-", "_"), description=remote.description) for remote in remotes],
    )

    response = await router.run("Prepare a blog post about hosted agents in Microsoft Foundry")
    print(response.text)


asyncio.run(main())
