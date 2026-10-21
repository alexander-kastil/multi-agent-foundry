"""
OpenTelemetry observability demo with Azure AI Foundry SDK.

This demo shows how to add basic observability to Azure AI operations
using OpenTelemetry for tracking and monitoring.
"""

import asyncio
import os
from datetime import datetime
from dotenv import load_dotenv

from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import ConsoleSpanExporter, SimpleSpanProcessor

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")


def setup_telemetry():
    """Setup OpenTelemetry with console exporter."""
    provider = TracerProvider()
    processor = SimpleSpanProcessor(ConsoleSpanExporter())
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)
    return trace.get_tracer(__name__)


async def main():
    """Interactive demo with OpenTelemetry observability."""
    
    print("\n" + "="*70)
    print("DEMO: OpenTelemetry Observability (Azure AI Foundry SDK)")
    print("="*70)
    
    # Setup telemetry
    tracer = setup_telemetry()
    
    print("\nOpenTelemetry configured with console exporter")
    print("Watch for span data in the output below\n")
    
    with tracer.start_as_current_span("agent_demo") as demo_span:
        demo_span.set_attribute("demo.type", "observability")
        demo_span.set_attribute("demo.start_time", datetime.now().isoformat())
        
        async with DefaultAzureCredential() as credential:
            async with AIProjectClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as project_client:
                
                async with AgentsClient(
                    endpoint=PROJECT_ENDPOINT,
                    credential=credential
                ) as agents_client:
                    
                    with tracer.start_as_current_span("create_agent") as span:
                        span.set_attribute("agent.model", MODEL_DEPLOYMENT)
                        
                        print("Creating agent...")
                        agent = await agents_client.create_agent(
                            model=MODEL_DEPLOYMENT,
                            name="Observability Demo Agent",
                            instructions="You are a helpful assistant. Be concise.",
                            description="Agent with observability"
                        )
                        
                        span.set_attribute("agent.id", agent.id)
                        print(f"Agent created: {agent.id}")
                    
                    with tracer.start_as_current_span("create_thread") as span:
                        thread = await agents_client.create_thread()
                        span.set_attribute("thread.id", thread.id)
                        print(f"Thread created: {thread.id}")
                    
                    print("\n" + "="*70)
                    print("Interactive Chat (Type 'quit' to exit)")
                    print("="*70)
                    print("Watch the console for span telemetry data\n")
                    
                    message_count = 0
                    
                    while True:
                        try:
                            user_input = input("You: ").strip()
                            if not user_input:
                                continue
                            if user_input.lower() in ['quit', 'exit', 'q']:
                                print("\nGoodbye!")
                                break
                            
                            message_count += 1
                            
                            with tracer.start_as_current_span("process_message") as span:
                                span.set_attribute("message.number", message_count)
                                span.set_attribute("message.length", len(user_input))
                                
                                # Create message
                                with tracer.start_as_current_span("create_message"):
                                    await agents_client.create_message(
                                        thread_id=thread.id,
                                        role="user",
                                        content=user_input
                                    )
                                
                                # Create and process run
                                print("Agent: ", end="", flush=True)
                                
                                response_text = ""
                                with tracer.start_as_current_span("run_agent") as run_span:
                                    async with await agents_client.create_run_stream(
                                        thread_id=thread.id,
                                        assistant_id=agent.id
                                    ) as stream:
                                        async for event in stream:
                                            if event.event == "thread.message.delta":
                                                if hasattr(event.data, 'delta') and hasattr(event.data.delta, 'content'):
                                                    for content in event.data.delta.content:
                                                        if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                                            text = content.text.value
                                                            print(text, end="", flush=True)
                                                            response_text += text
                                    
                                    run_span.set_attribute("response.length", len(response_text))
                                
                                print("\n")
                                span.set_attribute("message.completed", True)
                        
                        except (KeyboardInterrupt, EOFError):
                            print("\n\nExiting...")
                            break
                        except Exception as e:
                            print(f"\nError: {e}\n")
                    
                    # Clean up
                    with tracer.start_as_current_span("cleanup"):
                        print("\nCleaning up...")
                        await agents_client.delete_agent(agent.id)
                        print("Agent deleted.")
                    
                    demo_span.set_attribute("demo.messages_processed", message_count)
                    demo_span.set_attribute("demo.end_time", datetime.now().isoformat())
    
    print("\n" + "="*70)
    print("DEMO COMPLETE")
    print("="*70)
    print("Telemetry data was exported to console")
    print("In production, export to Application Insights or other backends")
    print("="*70 + "\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
