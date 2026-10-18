import asyncio
import sys

from agent_framework.orchestrations import SequentialBuilder

from agents import make_agent

BRIEF = "Announce that our expense app now supports receipt scanning from the phone camera."


async def main() -> None:
    writer = make_agent("writer", "You write a three sentence product announcement from the brief.")
    reviewer = make_agent("reviewer", "You review the announcement above and list at most two concrete improvements.")
    polisher = make_agent("polisher", "You apply the review to the announcement and reply with the final text only.")

    workflow = SequentialBuilder(
        name="announcement_pipeline",
        participants=[writer, reviewer, polisher],
        intermediate_output_from=[writer, reviewer],
    ).build()

    result = await workflow.run(BRIEF)
    for event in result:
        if event.type == "intermediate":
            print(f"[intermediate] {event.executor_id}: {event.data.text}\n")
    print(f"[final] {result.get_outputs()[0].text}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
