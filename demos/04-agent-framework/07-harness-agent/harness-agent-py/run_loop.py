import asyncio
import sys

from agent_framework import set_agent_mode, todos_remaining, todos_remaining_message

from agent_factory import INSTRUCTIONS, build_harness
from trail_tools import get_stage_status

sys.stdout.reconfigure(encoding="utf-8")

STEP_BY_STEP = INSTRUCTIONS + " In execute mode complete exactly one todo per reply, then stop."
PLAN = (
    "Create one todo per stage for a Grossglockner trek, then stop. The stages are: "
    "Stage 1 Heiligenblut to Elberfelder Hut, Stage 2 Elberfelder Hut to Salm Hut, "
    "Stage 3 Salm Hut to Glocknerhaus, Stage 4 Glocknerhaus to Kals."
)


def done(session) -> str:
    items = session.state["todo"]["items"]
    return f"{sum(item['is_complete'] for item in items)} of {len(items)} todos complete"


async def run(label: str, **loop) -> None:
    agent = build_harness(tools=[get_stage_status], agent_instructions=STEP_BY_STEP, **loop)
    session = agent.create_session()
    await agent.run(PLAN, session=session)
    set_agent_mode(session, "execute")
    await agent.run("Work through the todo list. Check each stage with get_stage_status.", session=session)
    print(f"{label}: {done(session)}")


async def main() -> None:
    await run("without loop")
    await run(
        "with loop   ",
        loop_should_continue=todos_remaining(looping_modes=["execute"]),
        loop_next_message=todos_remaining_message,
        loop_max_iterations=6,
    )


asyncio.run(main())
