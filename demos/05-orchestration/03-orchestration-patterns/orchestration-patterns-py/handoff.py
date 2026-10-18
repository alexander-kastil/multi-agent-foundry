import asyncio
import sys
from typing import Annotated

from agent_framework import tool
from agent_framework.orchestrations import HandoffAgentUserRequest, HandoffBuilder

from agents import make_agent


APPROVE_REFUNDS = True


@tool(approval_mode="never_require")
def lookup_charges(customer: Annotated[str, "Customer name"]) -> str:
    """Returns the last charges for a customer."""
    return f"{customer}: 2 charges of 29.00 EUR on 2026-10-01, one is a duplicate."


@tool(approval_mode="always_require")
def issue_refund(customer: Annotated[str, "Customer name"], amount: Annotated[float, "Refund amount in EUR"]) -> str:
    """Refunds an amount to a customer."""
    return f"Refunded {amount:.2f} EUR to {customer}."


async def collect(stream) -> list:
    pending = []
    async for event in stream:
        if event.type == "handoff_sent":
            print(f"[handoff] {event.data.source} -> {event.data.target}")
        elif event.type == "request_info":
            pending.append(event)
    return pending


async def main() -> None:
    triage = make_agent(
        "triage_agent",
        "You are frontline support. Hand off billing questions to billing_agent and app errors to technical_agent. Never answer them yourself.",
        description="Routes customers to the right specialist",
        handoff=True,
    )
    billing = make_agent(
        "billing_agent",
        "You own every billing question and never hand it back. Look up the charges for the customer, refund any duplicate charge, then confirm in two sentences. Hand back to triage_agent only for non-billing topics.",
        tools=[lookup_charges, issue_refund],
        description="Handles invoices, charges and refunds",
        handoff=True,
    )
    technical = make_agent(
        "technical_agent",
        "You resolve technical problems with the expense app in two sentences.",
        description="Handles app errors and crashes",
        handoff=True,
    )

    workflow = (
        HandoffBuilder(name="support_handoff", participants=[triage, billing, technical])
        .with_start_agent(triage)
        .add_handoff(triage, [billing, technical])
        .add_handoff(billing, [triage])
        .add_handoff(technical, [triage])
        .build()
    )

    replies = iter(["No, that is all, thank you."])
    pending = await collect(workflow.run("I am Dana Reyes and I was charged twice for my subscription this month.", stream=True))
    while pending:
        responses = {}
        for request in pending:
            if isinstance(request.data, HandoffAgentUserRequest):
                last = request.data.agent_response.messages[-1]
                print(f"[{last.author_name}] {last.text}")
                reply = next(replies, None)
                if reply is None:
                    responses[request.request_id] = HandoffAgentUserRequest.terminate()
                else:
                    print(f"[user] {reply}")
                    responses[request.request_id] = HandoffAgentUserRequest.create_response(reply)
            else:
                call = request.data.function_call
                print(f"[approval] {call.name}({call.parse_arguments()}) {'approved' if APPROVE_REFUNDS else 'rejected'}")
                responses[request.request_id] = request.data.to_function_approval_response(approved=APPROVE_REFUNDS)
        pending = await collect(workflow.run(responses=responses, stream=True))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
