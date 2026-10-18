import asyncio
import sys
import time

from agent_framework import AgentContext

from trail_agent import build_agent

sys.stdout.reconfigure(encoding="utf-8")


async def timing_middleware(context: AgentContext, call_next) -> None:
    started = time.perf_counter()
    print(f"[agent] start, {len(context.messages)} message(s), stream={context.stream}")
    await call_next()
    print(f"[agent] done in {time.perf_counter() - started:.1f}s")


async def main() -> None:
    agent = build_agent(middleware=[timing_middleware])
    result = await agent.run("Is the Wachau valley loop open?")
    print(result.text)


asyncio.run(main())
