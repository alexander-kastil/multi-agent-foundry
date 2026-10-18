import asyncio
import sys
from pathlib import Path

from agent_framework import Agent, SkillsProvider

from agent_factory import build_client
from approval_loop import run_with_approvals
from script_runner import run_script

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    skills = SkillsProvider.from_paths(
        skill_paths=Path(__file__).parent / "skills",
        script_runner=run_script,
    )

    agent = Agent(
        client=build_client(),
        name="HutDesk",
        instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
        context_providers=[skills],
    )

    session = agent.create_session()
    result, steps = await run_with_approvals(
        agent,
        "Quote 3 nights for 4 hikers in winter and tell me the cancellation rules.",
        session,
        approve=sys.argv[1] != "reject" if len(sys.argv) > 1 else True,
    )
    for step in steps:
        print(f"decided: {step}")
    print(result.text)


asyncio.run(main())
