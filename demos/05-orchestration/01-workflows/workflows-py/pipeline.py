import asyncio

from agent_framework import Executor, WorkflowBuilder, WorkflowContext, executor, handler
from typing_extensions import Never

from invoices import RAW_INVOICES, Invoice


class ParseInvoice(Executor):
    @handler
    async def parse(self, line: str, ctx: WorkflowContext[Invoice]) -> None:
        number, vendor, description, amount = line.split(",")
        await ctx.send_message(Invoice(number, vendor, description, float(amount)))


@executor(id="add_vat")
async def add_vat(invoice: Invoice, ctx: WorkflowContext[Invoice]) -> None:
    invoice.amount = round(invoice.amount * 1.2, 2)
    await ctx.send_message(invoice)


@executor(id="format_receipt")
async def format_receipt(invoice: Invoice, ctx: WorkflowContext[Never, str]) -> None:
    await ctx.yield_output(f"{invoice.number} | {invoice.vendor} | {invoice.amount:.2f} EUR incl. VAT")


async def main() -> None:
    parse = ParseInvoice(id="parse_invoice")
    workflow = (
        WorkflowBuilder(start_executor=parse, output_from=[format_receipt])
        .add_edge(parse, add_vat)
        .add_edge(add_vat, format_receipt)
        .build()
    )
    for line in RAW_INVOICES[:2]:
        result = await workflow.run(line)
        print(result.get_outputs()[0])


if __name__ == "__main__":
    asyncio.run(main())
