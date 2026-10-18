import os
import time
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from openai import OpenAI  # type: ignore

# This demo shows how to send public image URLs to an Azure AI agent for analysis.
# It sends a URL-based image with text input via streaming responses API,
# and displays the agent's real-time description of the image.

# Clear the console to keep the output focused on the agent interaction
os.system('cls' if os.name == 'nt' else 'clear')

# Load environment variables from .env file
load_dotenv()
endpoint = os.getenv("PROJECT_ENDPOINT")
model = os.getenv("IMG_MODEL_DEPLOYMENT")  # image-capable model deployment name
delete_resources = os.getenv("DELETE", "true").lower() == "true"

print(f"Using endpoint: {endpoint}")
print(f"Using model: {model}")
print(f"Delete resources: {delete_resources}")

# Connect to the Azure AI Foundry project
project_client = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())
openai_client: OpenAI = project_client.get_openai_client()
with project_client:
    start = time.time()
    agent = project_client.agents.create_version(
        agent_name="image-url-agent",
        definition=PromptAgentDefinition(model=model, instructions="You are a helpful vision agent.")
    )
    print(f"Created agent: {agent.name} (version {agent.version})")

    image_url = "https://raw.githubusercontent.com/Azure-Samples/cognitive-services-sample-data-files/master/ComputerVision/Images/landmark.jpg"
    user_text = "Describe the landmark and its location."

    # Responses API vision input structure using image_url block
    vision_input = [
        {"role": "user", "content": [
            {"type": "input_text", "text": user_text},
            {"type": "input_image", "image_url": image_url}
        ]}
    ]

    try:
        response = openai_client.responses.create(
            input=vision_input,
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
        )
    except Exception as e:
        print(f"Vision response failed: {e}")
        if delete_resources:
            project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
            print("Deleted agent version after failure")
        response = None

    if response:
        print("agent: ", end='', flush=True)
        for event in response:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()
                duration = time.time() - start
                print(f"Response completed (took {duration:.2f}s)")
                if event.response.error:
                    print(f"Response error: {event.response.error}")
                break

    if delete_resources:
        project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
        print("Deleted agent version")
    else:
        print(f"Preserved agent: {agent.name}:{agent.version}")
