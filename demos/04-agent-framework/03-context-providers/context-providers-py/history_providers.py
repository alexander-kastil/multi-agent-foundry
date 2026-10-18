import asyncio
import sys

from agent_framework import Agent, InMemoryHistoryProvider

from settings import build_client

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    primary = InMemoryHistoryProvider("memory", load_messages=True)
    audit = InMemoryHistoryProvider("audit", load_messages=False, store_context_messages=True)

    agent = Agent(
        client=build_client(),
        name="TrailGuide",
        instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
        context_providers=[primary, audit],
    )

    session = agent.create_session()
    options = {"store": False}
    await agent.run("I hike with a dog and I dislike steep climbs.", session=session, options=options)
    result = await agent.run("Which kind of trail suits me?", session=session, options=options)
    print(result.text)

    for source_id, scoped in session.state.items():
        print(f"{source_id}: {len(scoped['messages'])} messages")


asyncio.run(main())
