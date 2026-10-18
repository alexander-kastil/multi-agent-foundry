import os
import base64
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry.instrumentation.openai_v2 import OpenAIInstrumentor
from opentelemetry import trace

# This demo shows how to add tracing and observability to Azure AI agent interactions.
# It configures OpenTelemetry with Azure Monitor, creates an agent, sends image input
# via streaming responses API, records tracing spans, and demonstrates monitoring capabilities.

asset_file_path = os.path.join(os.path.dirname(__file__), "assets/soi.jpg")

# Clear the console to keep the output focused on the agent interaction
os.system('cls' if os.name == 'nt' else 'clear')

# Load environment variables from .env file
load_dotenv()
endpoint = os.getenv("PROJECT_ENDPOINT")
model = os.getenv("MODEL_DEPLOYMENT")
delete_resources = os.getenv("DELETE", "true").lower() == "true"

print(f"Using endpoint: {endpoint}")
print(f"Using model: {model}")
print(f"Delete resources: {delete_resources}")

# Initialize the AI Project Client to get Application Insights connection string
project_client = AIProjectClient(
    credential=DefaultAzureCredential(),
    endpoint=endpoint,
)

# Get the Application Insights connection string for tracing
connection_string = project_client.telemetry.get_application_insights_connection_string()
print(f"Application Insights configured for tracing")

# Configure Azure Monitor with OpenTelemetry
configure_azure_monitor(connection_string=connection_string)

# Instrument OpenAI SDK to enable tracing
OpenAIInstrumentor().instrument()

# Get a tracer instance for custom spans
tracer = trace.get_tracer(__name__)

project_client = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())
openai_client = project_client.get_openai_client()

with project_client:
    # Create agent span
    with tracer.start_as_current_span("create_agent"):
        agent = project_client.agents.create_version(
            agent_name="agent-tracing",
            definition=PromptAgentDefinition(model=model, instructions="You are helpful agent")
        )
        print(f"Created agent {agent.name}:{agent.version}")
        span = trace.get_current_span()
        span.set_attribute("agent.name", agent.name)
        span.set_attribute("agent.version", agent.version)
        span.set_attribute("agent.model", model)

    # Encode image with span
    with tracer.start_as_current_span("encode_image"):
        with open(asset_file_path, "rb") as f:
            base64_image = base64.b64encode(f.read()).decode("utf-8")
        span = trace.get_current_span()
        span.set_attribute("image.path", asset_file_path)
        span.set_attribute("image.size.bytes", len(base64_image))

    # Create response with span
    with tracer.start_as_current_span("create_response"):
        input_message = "Hello, what is in the image?"
        data_url = f"data:image/jpeg;base64,{base64_image}"
        response = openai_client.responses.create(
            input=[{
                "role": "user",
                "content": [
                    {"type": "input_text", "text": input_message},
                    {"type": "input_image", "image_url": data_url}
                ]
            }],
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
        )

    # Output handling span
    with tracer.start_as_current_span("output_parse"):
        message_count = 0
        print("agent: ", end='', flush=True)
        for event in response:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
                message_count += 1  # Count deltas as messages for simplicity
            elif event.type == "response.completed":
                print()
                break
        span = trace.get_current_span()
        span.set_attribute("messages.count", message_count)
        span.set_attribute("response.status", event.response.status)
        if event.response.error:
            span.set_attribute("response.error", str(event.response.error))

    # Cleanup span
    with tracer.start_as_current_span("cleanup"):
        if delete_resources:
            project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
            print("Deleted agent version")
            trace.get_current_span().set_attribute("cleanup.deleted", True)
        else:
            print(f"Preserved agent {agent.name}:{agent.version}")
            trace.get_current_span().set_attribute("cleanup.deleted", False)

print("\nTracing complete. View traces in Azure AI Foundry portal under Tracing section.")
