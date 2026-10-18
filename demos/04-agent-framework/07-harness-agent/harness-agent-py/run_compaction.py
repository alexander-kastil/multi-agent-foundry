import asyncio
import sys

from agent_factory import build_harness
from trail_tools import get_stage_report

sys.stdout.reconfigure(encoding="utf-8")

QUESTIONS = [f"What is the wind at 09:00 on stage {stage}? Use get_stage_report." for stage in (1, 2, 3, 4, 1, 2)]


async def run(label: str, **overrides) -> None:
    agent = build_harness(tools=[get_stage_report], disable_todo=True, disable_mode=True, **overrides)
    session = agent.create_session()
    per_turn = []
    for question in QUESTIONS:
        result = await agent.run(question, session=session)
        per_turn.append((result.usage_details or {}).get("input_token_count", 0))
    print(f"{label}: {per_turn}")


async def main() -> None:
    await run("no compaction  ", disable_compaction=True)
    await run("with compaction", max_context_window_tokens=4000, max_output_tokens=500)


asyncio.run(main())
