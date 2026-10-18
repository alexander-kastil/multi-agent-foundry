import asyncio
import os

from agent_framework import AgentExecutorRequest, AgentExecutorResponse, Message, WorkflowBuilder, WorkflowContext, executor
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from typing_extensions import Never

from invoices import RAW_INVOICES, Invoice

load_dotenv(override=True)

REVIEWER_INSTRUCTIONS = (
    "You are an accounts payable reviewer. Given an invoice, reply with one sentence: "
    "APPROVE or ESCALATE followed by a short reason. Escalate any amount above 10000."
)


@executor(id="parse_invoice")
async def parse_invoice(line: str, ctx: WorkflowContext[AgentExecutorRequest]) -> None:
    number, vendor, description, amount = line.split(",")
    invoice = Invoice(number, vendor, description, float(amount))
    text = f"Invoice {invoice.number} from {invoice.vendor} for {invoice.description}: {invoice.amount} EUR"
    await ctx.send_message(AgentExecutorRequest(messages=[Message("user", [text])], should_respond=True))


@executor(id="publish_decision")
async def publish_decision(response: AgentExecutorResponse, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(response.agent_response.text)


async def main() -> None:
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    reviewer = client.as_agent(name="invoice_reviewer", instructions=REVIEWER_INSTRUCTIONS)
    workflow = (
        WorkflowBuilder(start_executor=parse_invoice, output_from=[publish_decision])
        .add_edge(parse_invoice, reviewer)
        .add_edge(reviewer, publish_decision)
        .build()
    )
    for line in (RAW_INVOICES[0], RAW_INVOICES[2]):
        result = await workflow.run(line)
        print(result.get_outputs()[0])


if __name__ == "__main__":
    asyncio.run(main())
