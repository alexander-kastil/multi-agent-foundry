import os, base64
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

# This demo shows how to send image input as a base64 data URL to an Azure AI agent.
# It converts a local image to base64, sends it with text input via streaming responses API,
# and displays the agent's image analysis in real-time.

asset_file_path = os.path.join(os.path.dirname(__file__), "assets/soi.jpg")

def image_to_base64(image_path: str) -> str:

    if not os.path.isfile(image_path):
        raise FileNotFoundError(f"File not found at: {image_path}")

    try:
        with open(image_path, "rb") as image_file:
            file_data = image_file.read()
        return base64.b64encode(file_data).decode("utf-8")
    except Exception as exc:
        raise OSError(f"Error reading file '{image_path}'") from exc


# Clear the console to keep the output focused on the agent interaction
os.system('cls' if os.name == 'nt' else 'clear')

load_dotenv()
endpoint = os.getenv("PROJECT_ENDPOINT")
model = os.getenv("MODEL_DEPLOYMENT")
delete_resources = os.getenv("DELETE", "true").lower() == "true"

print(f"Using endpoint: {endpoint}")
print(f"Using model: {model}")
print(f"Delete resources: {delete_resources}")

project_client = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())
openai_client = project_client.get_openai_client()

with project_client:
    agent = project_client.agents.create_version(
        agent_name="base64-agent",
        definition=PromptAgentDefinition(
            model=model,
            instructions="You are helpful agent"
        )
    )
    print(f"Created agent: {agent.name}:{agent.version}")

    input_message = "Hello, what is in the image ?"
    image_base64 = image_to_base64(asset_file_path)
    data_url = f"data:image/jpeg;base64,{image_base64}"

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
    print(f"Response started...")
    print("agent: ", end='', flush=True)

    for event in response:
        if event.type == "response.output_text.delta":
            print(event.delta, end='', flush=True)
        elif event.type == "response.completed":
            print()
            print(f"Response completed")
            if event.response.error:
                print(f"Response error: {event.response.error}")
            break

    if delete_resources:
        project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
        print("Deleted agent version")
    else:
        print(f"Preserved agent: {agent.name}:{agent.version}")
