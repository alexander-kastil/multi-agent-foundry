import asyncio
import sys

from agent_framework.orchestrations import GroupChatBuilder, GroupChatState

from agents import responses_agent

TASK = "Write a one sentence slogan for an expense app that scans receipts with the phone camera."


def round_robin(state: GroupChatState) -> str:
    names = list(state.participants.keys())
    return names[state.current_round % len(names)]


async def print_run(workflow) -> None:
    result = await workflow.run(TASK)
    for event in result:
        if event.type == "intermediate":
            print(f"[{event.executor_id}] {event.data.text}")
    print(f"[final] {result.get_outputs()[0].text}")


async def main() -> None:
    writer = responses_agent("Writer", "You write one slogan per turn. Revise it when the reviewer gives feedback.", "Writes slogans")
    reviewer = responses_agent("Reviewer", "You review the latest slogan in one sentence. Say APPROVED when it is clear and short.", "Reviews slogans")

    print("Selection function, four turns")
    selector_chat = GroupChatBuilder(
        name="slogan_round_robin",
        participants=[writer, reviewer],
        selection_func=round_robin,
        termination_condition=lambda conversation: sum(1 for m in conversation if m.role == "assistant") >= 4,
        intermediate_output_from=[writer, reviewer],
    ).build()
    await print_run(selector_chat)

    print("\nAgent orchestrator picks the speaker")
    orchestrator = responses_agent(
        "Orchestrator",
        "You coordinate a slogan team. Let Writer go first, then Reviewer. Finish as soon as Reviewer says APPROVED.",
        "Selects the next speaker",
    )
    agent_chat = GroupChatBuilder(
        name="slogan_orchestrated",
        participants=[writer, reviewer],
        orchestrator_agent=orchestrator,
        termination_condition=lambda conversation: sum(1 for m in conversation if m.role == "assistant") >= 6,
        intermediate_output_from=[writer, reviewer],
    ).build()
    await print_run(agent_chat)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
