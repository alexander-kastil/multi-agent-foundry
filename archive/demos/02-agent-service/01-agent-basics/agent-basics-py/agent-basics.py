import os
import time
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

# This demo creates a basic Azure AI agent and demonstrates streaming responses.
# It creates an agent, sends a user input via the responses API with streaming enabled,
# prints the agent's response in real-time, and cleans up resources.

def main():
    # Clear console
    os.system('cls' if os.name == 'nt' else 'clear')

    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    delete_resources = os.getenv("DELETE", "true").lower() == "true"

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Delete resources: {delete_resources}")

    # Initialize project client and OpenAI responses client
    project_client = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())
    openai_client = project_client.get_openai_client()

    with project_client:
        start = time.time()
        # Create versioned agent (prompt kind)
        agent = project_client.agents.create_version(
            agent_name="basic-agent",
            definition=PromptAgentDefinition(
                model=model,
                instructions="You are helpful agent"
            )
        )
        print(f"Created agent: {agent.name} (version {agent.version})")

        # Create response with user input (streaming enabled)
        user_input = "Hello, tell me a joke"
        response = openai_client.responses.create(
            input=user_input,
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
        )

        # Status / timing
        print(f"Response started...")
        print("agent: ", end='', flush=True)

        # Stream output items
        for event in response:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()  # newline after completion
                duration = time.time() - start
                print(f"Response completed (took {duration:.2f}s)")
                if event.response.error:
                    print(f"Response error: {event.response.error}")
                break

        # Cleanup based on DELETE flag
        if delete_resources:
            project_client.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
            print("Deleted agent version")
        else:
            print(f"Preserved agent: {agent.name}:{agent.version}")


if __name__ == '__main__':
    main()
