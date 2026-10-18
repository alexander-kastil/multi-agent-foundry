import asyncio
import sys

from agent_framework import Agent, AgentSession, FileHistoryProvider

from settings import build_client

sys.stdout.reconfigure(encoding="utf-8")


async def main(session_id: str, message: str) -> None:
    agent = Agent(
        client=build_client(),
        name="TrailGuide",
        instructions="You are a trail guide for Contoso Outdoor. Answer in two sentences.",
        context_providers=[FileHistoryProvider("history")],
    )

    session = AgentSession(session_id=session_id)
    result = await agent.run(message, session=session, options={"store": False})
    print(result.text)


asyncio.run(main(sys.argv[1], sys.argv[2]))
