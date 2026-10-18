import asyncio
import os
import sys
from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from agent_framework.observability import configure_otel_providers, get_tracer
from azure.identity.aio import DefaultAzureCredential
from dotenv import load_dotenv
from opentelemetry import trace
from opentelemetry.sdk.trace.export import SimpleSpanProcessor
from telemetry import SpanCollector

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

ORDERS = {"12345": "shipped on 2026-10-14, arriving 2026-10-19", "98765": "processing"}


@tool(approval_mode="never_require")
def get_order_status(order_id: str) -> str:
    """Look up the status of a customer order."""
    return ORDERS.get(order_id, "unknown order")


async def main():
    collector = SpanCollector()
    async with DefaultAzureCredential() as credential:
        client = FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=credential,
        )
        if os.getenv("EXPORT_TO_APP_INSIGHTS", "false").lower() == "true":
            await client.configure_azure_monitor(sampling_ratio=1.0)
            trace.get_tracer_provider().add_span_processor(SimpleSpanProcessor(collector))
        else:
            configure_otel_providers(exporters=[collector])
        agent = Agent(
            client=client,
            name="order-agent",
            instructions="You answer order questions in one sentence. Use the tool for order status.",
            tools=[get_order_status],
        )
        with get_tracer().start_as_current_span("customer_request"):
            result = await agent.run("Where is my order 12345?")
        print(result.text)
    trace.get_tracer_provider().force_flush()
    print()
    print(collector.waterfall())


if __name__ == "__main__":
    asyncio.run(main())
