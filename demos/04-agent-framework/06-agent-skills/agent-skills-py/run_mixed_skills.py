import asyncio
import sys
from pathlib import Path

from agent_framework import (
    Agent,
    AggregatingSkillsSource,
    DeduplicatingSkillsSource,
    FileSkillsSource,
    InMemorySkillsSource,
    SkillsProvider,
    ToolApprovalMiddleware,
)

from agent_factory import build_client
from altitude_skill import altitude_skill
from approval_loop import run_with_approvals
from script_runner import run_script

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    skills = SkillsProvider(
        DeduplicatingSkillsSource(
            AggregatingSkillsSource([
                FileSkillsSource(Path(__file__).parent / "skills", script_runner=run_script),
                InMemorySkillsSource([altitude_skill]),
            ])
        )
    )

    agent = Agent(
        client=build_client(),
        name="HutDesk",
        instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
        context_providers=[skills],
        middleware=[ToolApprovalMiddleware(auto_approval_rules=[SkillsProvider.read_only_tools_auto_approval_rule])],
    )

    session = agent.create_session()
    result, steps = await run_with_approvals(
        agent,
        "The Similaun Hut is at 3019 meters. How many feet is that, and what should I do if a group member is hurt there?",
        session,
    )
    for step in steps:
        print(f"asked for approval: {step}")
    print(result.text)


asyncio.run(main())
