import asyncio

from agent_framework import RunContext, step, workflow

from agents import make_agent

writer = make_agent("writer", "You write a two sentence product announcement for the topic you are given.")
reviser = make_agent("reviser", "You rewrite the announcement so that it follows the reviewer feedback. Reply with the new text only.")


@step
async def write_draft(topic: str) -> str:
    return (await writer.run(topic)).text


@step
async def revise_draft(draft: str, feedback: str) -> str:
    return (await reviser.run(f"Announcement: {draft}\nFeedback: {feedback}")).text


@workflow(name="announcement_review", description="Draft, pause for a human, revise")
async def announcement_review(topic: str, ctx: RunContext) -> str:
    draft = await write_draft(topic)
    feedback = await ctx.request_info({"draft": draft}, response_type=str, request_id="review")
    return await revise_draft(draft, feedback)


async def main() -> None:
    pipeline = announcement_review.build()

    first = await pipeline.run("Receipt scanning in the expense app")
    print(f"state after first run: {first.get_final_state()}")
    request = first.get_request_info_events()[0]
    print(f"request {request.request_id}: {request.data['draft']}")

    print("resuming with feedback")
    async for event in pipeline.run(responses={"review": "Make it a single sentence and mention saving time."}, stream=True):
        if event.type in ("executor_invoked", "executor_bypassed"):
            print(f"  {event.type} {event.executor_id}")
        elif event.type == "output":
            print(f"final: {event.data}")


if __name__ == "__main__":
    asyncio.run(main())
