import asyncio
import sys

from agent_framework import AgentContext

from trail_agent import build_agent

sys.stdout.reconfigure(encoding="utf-8")


def tracer(name: str):
    async def middleware(context: AgentContext, call_next) -> None:
        print(f"-> {name}")
        await call_next()
        print(f"<- {name}")

    return middleware


async def main() -> None:
    agent = build_agent(middleware=[tracer("A1"), tracer("A2")])
    result = await agent.run("Is the Wachau valley loop open?", middleware=[tracer("R1"), tracer("R2")])
    print(result.text)


asyncio.run(main())
