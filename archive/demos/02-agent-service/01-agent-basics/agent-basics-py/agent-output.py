import os
import time
import io
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from openai import OpenAI  # type: ignore
import qrcode
from datetime import datetime
from azure.storage.blob import BlobServiceClient

# This demo shows post-processing of agent responses and external integration.
# It creates an agent, streams its response, captures the output, generates a QR code
# from user input, and uploads it to Azure Blob Storage.

def main():

    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    storage_connection_string = os.getenv("STORAGE_CONNECTION_STRING")
    storage_container_name = os.getenv("STORAGE_CONTAINER_NAME")

    delete_resources = os.getenv("DELETE", "true").lower() == "true"
    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Delete resources: {delete_resources}")

    # Initialize new project + responses client (no threads/runs now)
    project_client = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())
    openai_client: OpenAI = project_client.get_openai_client()
    with project_client:
        start = time.time()
        agent = project_client.agents.create_version(
            agent_name="output-agent",
            definition=PromptAgentDefinition(model=model, instructions="You are helpful agent")
        )
        print(f"Created agent: {agent.name} (version {agent.version})")

        try:
            response = openai_client.responses.create(
                input="Hello",
                stream=True,
                extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
            )
        except Exception as e:
            print(f"Response creation failed: {e}")
            if delete_resources:
                project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
                print("Deleted agent version after failure")
            response = None

        agent_response = ""
        if response:
            print("agent: ", end='', flush=True)
            for event in response:
                if event.type == "response.output_text.delta":
                    delta = event.delta
                    print(delta, end='', flush=True)
                    agent_response += delta
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

        # Ask for QR code content (after agent interaction)
        user_input = input("What do you want to encode? Press Enter for default: https://www.integrations.at\n")
        qr_content = user_input if user_input.strip() else "https://www.integrations.at"

        # Generate QR code
        qr = qrcode.QRCode(version=1, box_size=10, border=5)
        qr.add_data(qr_content)
        qr.make(fit=True)
        img = qr.make_image(fill='black', back_color='white')

        # Save to bytes
        img_bytes = io.BytesIO()
        img.save(img_bytes, format='JPEG')
        img_bytes.seek(0)

        # Upload to blob storage
        date_str = datetime.now().strftime("%Y%m%d")
        blob_name = f"qr{date_str}.jpg"

        blob_service_client = BlobServiceClient.from_connection_string(storage_connection_string)
        container_client = blob_service_client.get_container_client(storage_container_name)
        try:
            container_client.create_container()
            print(f"Created container {storage_container_name}")
        except Exception as e:
            if "ContainerAlreadyExists" not in str(e):
                raise
        blob_client = blob_service_client.get_blob_client(container=storage_container_name, blob=blob_name)
        blob_client.upload_blob(img_bytes, overwrite=True)

        # Get download URL
        download_url = f"https://procodestorageacct.blob.core.windows.net/{storage_container_name}/{blob_name}"
        print(f"QR Code uploaded. Download URL: {download_url}")


if __name__ == '__main__':
    main()
