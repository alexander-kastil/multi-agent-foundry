import json
import os
import sys
import httpx
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.ai.projects.telemetry import AIProjectInstrumentor
from azure.identity import DefaultAzureCredential
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry import trace
from opentelemetry.sdk.trace.export import SimpleSpanProcessor
from opentelemetry.trace import SpanKind, Status, StatusCode
from downstream_agent import start_agent_service
from propagation import inject_headers
from structured_log import StructuredLogger
from telemetry import SpanCollector

ROUTER_PROMPT = (
    "Classify the customer message. Reply with JSON only and no markdown: "
    '{"target_agent": "product-search-agent" or "order-agent" or "payment-agent", "confidence": 0.0 to 1.0}.'
)

REQUESTS = [
    "Do you have sugar-free dark chocolate under 20 dollars?",
    "Where is my order 12345?",
    "I want to update the credit card on my account.",
]

SPECIALISTS = {
    "m7-product-search-agent": "You are a chocolate shop product search assistant. Answer in two sentences.",
    "m7-order-agent": "You are an order status assistant. Invent plausible tracking details. Answer in two sentences.",
}

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
):
    configure_azure_monitor(
        connection_string=project_client.telemetry.get_application_insights_connection_string(),
        sampling_ratio=1.0,
    )
    AIProjectInstrumentor().instrument()
    collector = SpanCollector()
    trace.get_tracer_provider().add_span_processor(SimpleSpanProcessor(collector))
    tracer = trace.get_tracer("orchestrator")
    orchestrator_log = StructuredLogger("orchestrator-agent", "1.0.0")
    openai_client = project_client.get_openai_client()
    model = os.environ["MODEL_DEPLOYMENT_NAME"]
    versions = {}
    for name, instructions in SPECIALISTS.items():
        agent = project_client.agents.create_version(
            agent_name=name,
            definition=PromptAgentDefinition(model=model, instructions=instructions),
        )
        versions[name] = agent.version

    server = start_agent_service(openai_client, versions)
    base_url = f"http://127.0.0.1:{server.server_address[1]}"

    for index, question in enumerate(REQUESTS):
        session_id = f"session-{index}"
        with tracer.start_as_current_span("customer_request") as root:
            root.set_attribute("session.id", session_id)
            with tracer.start_as_current_span(f"chat {model}") as routing_span:
                routing_span.set_attribute("gen_ai.operation.name", "chat")
                routing_span.set_attribute("gen_ai.agent.name", "orchestrator-agent")
                response = openai_client.responses.create(model=model, instructions=ROUTER_PROMPT, input=question)
                decision = json.loads(response.output_text)
                orchestrator_log.log(
                    "LLM_ROUTING",
                    "success",
                    session_id=session_id,
                    customer_tier="premium",
                    input_token_count=response.usage.input_tokens,
                    output_token_count=response.usage.output_tokens,
                    model_id=model,
                    output=decision,
                )
            target = "m7-" + decision["target_agent"]
            with tracer.start_as_current_span(f"orchestrator_to_{target}", kind=SpanKind.CLIENT) as call_span:
                headers = inject_headers()
                call_span.set_attribute("gen_ai.agent.name", "orchestrator-agent")
                call_span.set_attribute("agent.downstream", target)
                reply = httpx.post(
                    f"{base_url}/{target}",
                    json={"input": question, "session_id": session_id, "customer_tier": "premium"},
                    headers=headers,
                    timeout=120,
                )
                if reply.status_code == 200:
                    print(f"{target}: {reply.json()['answer']}")
                else:
                    call_span.set_status(Status(StatusCode.ERROR))
                    orchestrator_log.log("AGENT_CALL", "error", error_code=reply.json()["error"], downstream=target)

    server.shutdown()
    for name, version in versions.items():
        project_client.agents.delete_version(agent_name=name, agent_version=version)

trace.get_tracer_provider().force_flush()
print()
print(collector.waterfall())
print()
print(f"Requests: {len(REQUESTS)}  Distinct trace IDs: {len(collector.trace_ids())}")
print(f"Flagged spans: {[span.name for span in collector.flagged()]}")
