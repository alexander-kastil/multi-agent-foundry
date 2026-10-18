import asyncio
import json
import sys

from agent_framework import Agent, AgentSession

from preference_provider import PreferenceProvider
from settings import build_client

sys.stdout.reconfigure(encoding="utf-8")


def build_agent() -> Agent:
    return Agent(
        client=build_client(),
        name="TrailGuide",
        instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
        context_providers=[PreferenceProvider()],
    )


async def main() -> None:
    agent = build_agent()
    session = agent.create_session()
    options = {"store": False}

    await agent.run("My favorite trail surface is gravel. My favorite season is autumn.", session=session, options=options)
    print(json.dumps(session.state["preferences"], indent=2))

    stored = json.dumps(session.to_dict())
    restored = AgentSession.from_dict(json.loads(stored))

    result = await build_agent().run("Suggest a weekend hike.", session=restored, options=options)
    print(result.text)


asyncio.run(main())
