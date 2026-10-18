import asyncio
import time
from dataclasses import dataclass

from agent_framework import WorkflowBuilder, WorkflowContext, executor
from typing_extensions import Never

from invoices import RAW_INVOICES, Invoice


@dataclass
class Check:
    name: str
    passed: bool


@executor(id="parse_invoice")
async def parse_invoice(line: str, ctx: WorkflowContext[Invoice]) -> None:
    number, vendor, description, amount = line.split(",")
    await ctx.send_message(Invoice(number, vendor, description, float(amount)))


@executor(id="duplicate_check")
async def duplicate_check(invoice: Invoice, ctx: WorkflowContext[Check]) -> None:
    await asyncio.sleep(1)
    await ctx.send_message(Check("duplicate", True))


@executor(id="vendor_check")
async def vendor_check(invoice: Invoice, ctx: WorkflowContext[Check]) -> None:
    await asyncio.sleep(1)
    await ctx.send_message(Check("vendor", invoice.vendor != "Adatum Corp"))


@executor(id="budget_check")
async def budget_check(invoice: Invoice, ctx: WorkflowContext[Check]) -> None:
    await asyncio.sleep(1)
    await ctx.send_message(Check("budget", invoice.amount < 10000))


@executor(id="aggregate")
async def aggregate(checks: list[Check], ctx: WorkflowContext[Never, str]) -> None:
    failed = [check.name for check in checks if not check.passed]
    await ctx.yield_output("all checks passed" if not failed else f"failed: {', '.join(failed)}")


def build_workflow():
    return (
        WorkflowBuilder(start_executor=parse_invoice, output_from=[aggregate])
        .add_fan_out_edges(parse_invoice, [duplicate_check, vendor_check, budget_check])
        .add_fan_in_edges([duplicate_check, vendor_check, budget_check], aggregate)
        .build()
    )


async def main() -> None:
    workflow = build_workflow()
    started = time.perf_counter()
    async for event in workflow.run(RAW_INVOICES[2], stream=True):
        if event.type == "superstep_started":
            print(f"superstep {event.iteration} started at {time.perf_counter() - started:.1f}s")
        elif event.type == "executor_invoked":
            print(f"  invoked {event.executor_id}")
        elif event.type == "output":
            print(f"output: {event.data}")
    print(f"total {time.perf_counter() - started:.1f}s for three one-second checks")


if __name__ == "__main__":
    asyncio.run(main())
