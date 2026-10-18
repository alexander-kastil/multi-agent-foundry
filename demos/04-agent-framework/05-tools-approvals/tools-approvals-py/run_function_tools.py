import asyncio
import sys

from agent_factory import build_agent, tool_calls
from trail_tools import estimate_price, get_trail_status

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    agent = build_agent([get_trail_status, estimate_price])
    result = await agent.run("Is the Wachau valley loop open, and what do 2 nights for 3 hikers cost?")
    for call in tool_calls(result):
        print(f"tool call: {call}")
    print(result.text)


asyncio.run(main())
