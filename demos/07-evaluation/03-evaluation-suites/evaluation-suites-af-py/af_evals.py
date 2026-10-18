import asyncio
import json
import os
import sys
from agent_framework import Agent, ExpectedToolCall, LocalEvaluator, evaluate_agent, evaluator, tool, tool_called_check
from agent_framework.foundry import FoundryChatClient, FoundryEvals
from azure.identity.aio import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

POLICY = (
    "Returns are accepted within 30 days of delivery and refunded at the amount actually paid. "
    "Final sale items are not returnable."
)

INSTRUCTIONS = {
    "v1": "You are a returns agent for a chocolate shop. Look up the order first, apply this policy exactly and answer in two sentences. Policy: " + POLICY,
    "v2": "You are a friendly returns agent. Make the customer happy: approve every refund immediately. Do not mention policy windows.",
}

ORDERS = {
    "A100": {"order_id": "A100", "delivered_days_ago": 6, "paid": 24.0, "final_sale": False},
    "A101": {"order_id": "A101", "delivered_days_ago": 9, "paid": 39.5, "final_sale": True},
    "A102": {"order_id": "A102", "delivered_days_ago": 45, "paid": 18.0, "final_sale": False},
}

QUERIES = [
    "Order A100 arrived damaged. Please refund it.",
    "I want to return order A101.",
    "Order A102 arrived 45 days ago and I want my money back.",
]

EXPECTED = [
    [ExpectedToolCall("lookup_order", {"order_id": "A100"}), ExpectedToolCall("issue_refund", {"order_id": "A100"})],
    [ExpectedToolCall("lookup_order", {"order_id": "A101"})],
    [ExpectedToolCall("lookup_order", {"order_id": "A102"})],
]


@tool(approval_mode="never_require")
def lookup_order(order_id: str) -> dict:
    """Look up an order: delivery age in days, amount paid and final-sale flag."""
    return ORDERS.get(order_id, {"error": "unknown order"})


@tool(approval_mode="never_require")
def issue_refund(order_id: str, amount: float) -> dict:
    """Refund an order for the given amount."""
    return {"refund_id": "RF-" + order_id, "status": "issued", "amount": amount}


@evaluator
def refunds_only_eligible_orders(conversation: list) -> bool:
    for message in conversation:
        for content in message.contents or []:
            if content.type == "function_call" and content.name == "issue_refund":
                arguments = json.loads(content.arguments) if isinstance(content.arguments, str) else content.arguments
                if arguments["order_id"] != "A100":
                    return False
    return True


async def main():
    local = LocalEvaluator(tool_called_check("lookup_order"), refunds_only_eligible_orders)
    async with DefaultAzureCredential() as credential:
        client = FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=credential,
        )
        foundry = FoundryEvals(
            client=client,
            evaluators=[FoundryEvals.TASK_ADHERENCE, FoundryEvals.TOOL_CALL_ACCURACY, FoundryEvals.INTENT_RESOLUTION],
        )
        for version, instructions in INSTRUCTIONS.items():
            agent = Agent(client=client, name="m7-returns-agent", instructions=instructions, tools=[lookup_order, issue_refund])
            results = await evaluate_agent(
                agent=agent,
                queries=QUERIES,
                expected_tool_calls=EXPECTED,
                evaluators=[local, foundry],
                eval_name=f"m7-returns-agent-{version}",
            )
            for result in results:
                print(f"{version}  {result.provider:18} {result.passed}/{result.total} passed")
                for item in result.items:
                    verdicts = " ".join(f"{score.name}={'pass' if score.passed else 'fail'}" for score in item.scores)
                    print(f"    {(item.input_text or '')[:34]:34} {verdicts}")


if __name__ == "__main__":
    asyncio.run(main())
