import asyncio
import sys

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    agent = build_agent()

    session = agent.create_session()
    await agent.run("My name is Alex and I plan a kayak trip on the Danube.", session=session)
    with_session = await agent.run("Which river did I mention?", session=session)
    print(f"same session : {with_session.text}")

    without_session = await agent.run("Which river did I mention?")
    print(f"no session   : {without_session.text}")


asyncio.run(main())
