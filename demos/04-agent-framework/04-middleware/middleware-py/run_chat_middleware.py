import asyncio
import sys

from agent_framework import ChatContext

from trail_agent import build_agent

sys.stdout.reconfigure(encoding="utf-8")

calls = {"count": 0, "tokens": 0}


async def model_meter_middleware(context: ChatContext, call_next) -> None:
    calls["count"] += 1
    print(f"[chat] call {calls['count']}: {len(context.messages)} message(s) to the model")
    await call_next()
    usage = context.result.usage_details or {}
    calls["tokens"] += usage.get("total_token_count", 0)
    print(f"[chat] call {calls['count']}: {usage.get('total_token_count', 0)} tokens")


async def main() -> None:
    agent = build_agent(middleware=[model_meter_middleware])
    result = await agent.run("Is the Tauern high route open?")
    print(result.text)
    print(f"model calls: {calls['count']}, total tokens: {calls['tokens']}")


asyncio.run(main())
