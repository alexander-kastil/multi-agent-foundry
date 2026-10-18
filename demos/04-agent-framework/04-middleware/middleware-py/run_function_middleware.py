import asyncio
import sys

from agent_framework import FunctionInvocationContext

from trail_agent import build_agent

sys.stdout.reconfigure(encoding="utf-8")


async def tool_audit_middleware(context: FunctionInvocationContext, call_next) -> None:
    print(f"[tool] {context.function.name}({dict(context.arguments)})")
    await call_next()
    status = context.result[0].text
    print(f"[tool] result: {status}")
    if status.startswith("closed"):
        context.result = f"{status}; recommend an alternative route"
        print(f"[tool] rewritten: {context.result}")


async def main() -> None:
    agent = build_agent(middleware=[tool_audit_middleware])
    result = await agent.run("Can I hike the Dachstein traverse tomorrow?")
    print(result.text)


asyncio.run(main())
