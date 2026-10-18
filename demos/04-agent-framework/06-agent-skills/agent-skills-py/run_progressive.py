import asyncio
import sys
from pathlib import Path

from agent_framework import Agent, ChatContext, SkillsProvider

from agent_factory import build_client
from approval_loop import run_with_approvals
from script_runner import run_script

sys.stdout.reconfigure(encoding="utf-8")

calls = []


async def token_meter(context: ChatContext, call_next) -> None:
    await call_next()
    usage = context.result.usage_details or {}
    calls.append(usage.get("input_token_count", 0))


async def main() -> None:
    skills = SkillsProvider.from_paths(
        skill_paths=Path(__file__).parent / "skills",
        script_runner=run_script,
    )

    plain = Agent(
        client=build_client(),
        name="PlainDesk",
        instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
        middleware=[token_meter],
    )
    await plain.run("Say hello.")
    print(f"without skills: {calls[-1]} input tokens")

    calls.clear()
    agent = Agent(
        client=build_client(),
        name="HutDesk",
        instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
        context_providers=[skills],
        middleware=[token_meter],
    )

    session = agent.create_session()
    await agent.run("Say hello.", session=session)
    print(f"advertise only: {calls[-1]} input tokens")

    calls.clear()
    _, steps = await run_with_approvals(
        agent, "Quote 3 nights for 4 hikers in winter and tell me the cancellation rules.", agent.create_session()
    )
    for number, tokens in enumerate(calls, start=1):
        print(f"model call {number}: {tokens} input tokens")
    for step in steps:
        print(f"approved: {step[:90]}")

asyncio.run(main())
