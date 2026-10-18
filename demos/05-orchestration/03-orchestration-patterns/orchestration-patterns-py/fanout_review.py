import asyncio
import sys
import time

from agent_framework import AgentExecutorResponse
from agent_framework.orchestrations import ConcurrentBuilder

from agents import make_agent

PROPOSAL = "We want to let employees scan receipts with their phone and auto-approve expenses under 50 EUR."


async def main() -> None:
    finance = make_agent("finance", "You assess the cost and savings of the proposal in two sentences.")
    security = make_agent("security", "You assess the security and fraud risks of the proposal in two sentences.")
    compliance = make_agent("compliance", "You assess tax and data protection compliance of the proposal in two sentences.")
    participants = [finance, security, compliance]

    started = time.perf_counter()
    result = await ConcurrentBuilder(name="proposal_review", participants=participants).build().run(PROPOSAL)
    print(f"default aggregator, {time.perf_counter() - started:.1f}s")
    for message in result.get_outputs()[0].messages:
        print(f"[{message.author_name}] {message.text}")

    summarizer = make_agent("summarizer", "You merge expert opinions into one recommendation of at most 60 words.")

    async def summarize(results: list[AgentExecutorResponse]) -> str:
        opinions = "\n".join(f"{r.executor_id}: {r.agent_response.text}" for r in results)
        return (await summarizer.run(opinions)).text

    workflow = ConcurrentBuilder(name="proposal_review_summary", participants=participants).with_aggregator(summarize).build()
    result = await workflow.run(PROPOSAL)
    print("\ncustom aggregator")
    print(result.get_outputs()[0])


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
