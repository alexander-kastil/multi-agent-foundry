import asyncio

from agent_framework import Case, Default, WorkflowBuilder, WorkflowContext, executor
from typing_extensions import Never

from invoices import RAW_INVOICES, Invoice


@executor(id="parse_invoice")
async def parse_invoice(line: str, ctx: WorkflowContext[Invoice]) -> None:
    number, vendor, description, amount = line.split(",")
    await ctx.send_message(Invoice(number, vendor, description, float(amount)))


@executor(id="classify")
async def classify(invoice: Invoice, ctx: WorkflowContext[Invoice]) -> None:
    await ctx.send_message(invoice)


@executor(id="reject")
async def reject(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number}: rejected, amount {invoice.amount} is not valid")


@executor(id="auto_approve")
async def auto_approve(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number}: auto-approved ({invoice.amount:.2f})")


@executor(id="manager_review")
async def manager_review(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number}: sent to the manager ({invoice.amount:.2f})")


@executor(id="director_review")
async def director_review(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number}: sent to the director ({invoice.amount:.2f})")


def build_workflow():
    return (
        WorkflowBuilder(
            start_executor=parse_invoice,
            output_from=[reject, auto_approve, manager_review, director_review],
        )
        .add_edge(parse_invoice, classify, condition=lambda invoice: invoice.amount > 0)
        .add_edge(parse_invoice, reject, condition=lambda invoice: invoice.amount <= 0)
        .add_switch_case_edge_group(
            classify,
            [
                Case(condition=lambda invoice: invoice.amount < 1000, target=auto_approve),
                Case(condition=lambda invoice: invoice.amount < 10000, target=manager_review),
                Default(target=director_review),
            ],
        )
        .build()
    )


async def main() -> None:
    workflow = build_workflow()
    for line in RAW_INVOICES:
        result = await workflow.run(line)
        print(result.get_outputs()[0])


if __name__ == "__main__":
    asyncio.run(main())
