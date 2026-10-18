import asyncio
import sys
from typing import Annotated

from agent_framework import tool
from agent_framework.orchestrations import MagenticBuilder, MagenticPlanReviewRequest, MagenticProgressLedger

from agents import chat_completions_agent, responses_agent

TASK = (
    "Plan a one day team offsite in Vienna for 12 people with a total budget of 1500 EUR. "
    "Propose a venue, catering and a four item agenda, then verify the total stays within budget."
)


FEEDBACK = "Keep the venue under 400 EUR and add a short team building activity to the agenda."


@tool(approval_mode="never_require")
def sum_costs(items: Annotated[list[float], "Cost of each line item in EUR"]) -> str:
    """Adds up line item costs and returns the total in EUR."""
    return f"{sum(items):.2f} EUR"


def build_workflow(make_agent, enable_plan_review: bool = False):
    researcher = make_agent("Researcher", "You propose venue, catering and agenda options with plausible prices. You do not add up costs.", "Finds options and prices")
    analyst = make_agent("Analyst", "You verify budgets. Always call sum_costs for the total.", "Checks totals against the budget", tools=[sum_costs])
    manager = make_agent("Manager", "You coordinate the team to complete the task efficiently.", "Plans and delegates")
    return MagenticBuilder(
        name="offsite_planning",
        participants=[researcher, analyst],
        manager_agent=manager,
        enable_plan_review=enable_plan_review,
        max_round_count=8,
        max_stall_count=2,
        max_reset_count=1,
    ).build()


def describe(event) -> str:
    content = event.data.content
    if isinstance(content, MagenticProgressLedger):
        return f"satisfied={content.is_request_satisfied.answer} next={content.next_speaker.answer}"
    return f"{len(content.text)} characters, mentions 400 EUR: {'400' in content.text}"


async def run_with_review(workflow) -> str:
    responses = None
    revisions = 0
    while True:
        stream = workflow.run(stream=True, responses=responses) if responses else workflow.run(TASK, stream=True)
        review = None
        async for event in stream:
            if event.type == "magentic_orchestrator":
                print(f"[{event.data.event_type.name}] {describe(event)}")
            elif event.type == "request_info" and event.request_type is MagenticPlanReviewRequest:
                review = event
        result = await stream.get_final_response()
        if outputs := result.get_outputs():
            return outputs[-1].text
        request = review.data
        print(f"plan under review, revision {revisions}: stalled={request.is_stalled}")
        if revisions == 0:
            revisions += 1
            responses = {review.request_id: request.revise(FEEDBACK)}
        else:
            responses = {review.request_id: request.approve()}


async def main() -> None:
    use_responses = len(sys.argv) > 1 and sys.argv[1] == "responses"
    make_agent = responses_agent if use_responses else chat_completions_agent
    print(f"Magentic on {'a Responses' if use_responses else 'a Chat Completions'} client")
    workflow = build_workflow(make_agent, enable_plan_review=True)
    print(await run_with_review(workflow))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
