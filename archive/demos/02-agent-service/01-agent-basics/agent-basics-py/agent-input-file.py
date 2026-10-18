import os
import base64
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

# This demo demonstrates sending local image files as base64 to an Azure AI agent.
# It encodes an image file, sends it with text input via streaming responses API,
# and displays the agent's real-time analysis of the image content.

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

# Connect to the Azure AI Foundry project
project_client = AIProjectClient(
    endpoint=endpoint,
    credential=DefaultAzureCredential()
)

# Get the OpenAI client for conversations and responses
openai_client = project_client.get_openai_client()

with project_client:
    try:
        # Create agent using new create_version API
        agent = project_client.agents.create_version(
            agent_name="file-search-agent-mig",
            definition=PromptAgentDefinition(
                model=model,
                instructions="You are helpful agent",
            )
        )
        print(f"Created agent, agent ID: {agent.id}, name: {agent.name}, version: {agent.version}")

        # Encode image as base64
        with open(asset_file_path, "rb") as image_file:
            base64_image = base64.b64encode(image_file.read()).decode('utf-8')
        print(f"Encoded image as base64")

        # Create conversation with message containing image
        input_message = "Hello, what is in the image ?"
        
        # Create response with input containing base64 image
        response = openai_client.responses.create(
            input=[
                {
                    "role": "user",
                    "content": [
                        {"type": "input_text", "text": input_message},
                        {
                            "type": "input_image",
                            "image_url": f"data:image/jpeg;base64,{base64_image}"
                        }
                    ]
                }
            ],
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
        )

        print("agent: ", end='', flush=True)
        # Check response status
        for event in response:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()
                print(f"Response completed with ID: {event.response.id}")
                if event.response.status != "completed":
                    print(f"The response did not succeed: {event.response.status}")
                    if event.response.error:
                        print(f"Error: {event.response.error}")
                break

        # Delete the agent version based on DELETE setting
        if delete_resources:
            project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
            print("Deleted agent")
        else:
            print(f"Agent preserved: {agent.name}:{agent.version}")
    except Exception as e:
        print(f"Error occurred: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
