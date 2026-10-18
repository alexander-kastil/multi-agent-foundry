import os
import time
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

# This demo demonstrates agent versioning by creating multiple versions of an agent
# with different instructions and capabilities.

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

    agent_name = "agent-versions"

    with project_client:
        print(f"\n{'='*60}")
        print("AGENT VERSIONING DEMO")
        print(f"{'='*60}")

        # Create Version 1: Agent that tells jokes about birds
        print(f"\n📝 Creating {agent_name} Version 1...")
        agent_v1 = project_client.agents.create_version(
            agent_name=agent_name,
            definition=PromptAgentDefinition(
                model=model,
                instructions="You are a helpful agent that specializes in telling jokes about birds. Always respond with a funny joke about birds when asked."
            )
        )
        print(f"✅ Created agent: {agent_v1.name} (version {agent_v1.version})")

        # Execute Version 1
        print(f"\n🎭 Executing {agent_name} Version 1...")
        user_input_v1 = "Tell me a joke about birds"
        print(f"Prompt: {user_input_v1}")
        response_v1 = openai_client.responses.create(
            input=user_input_v1,
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent_v1.name, "version": agent_v1.version}}
        )

        print(f"Response: ", end='', flush=True)
        for event in response_v1:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()
                break

        # Create Version 2: Agent that writes poems about dogs
        print(f"\n📝 Creating {agent_name} Version 2...")
        agent_v2 = project_client.agents.create_version(
            agent_name=agent_name,
            definition=PromptAgentDefinition(
                model=model,
                instructions="You are a creative agent that specializes in writing short poems about dogs. Always respond with a short, rhyming poem about dogs when asked."
            )
        )
        print(f"✅ Created agent: {agent_v2.name} (version {agent_v2.version})")

        # Execute Version 2
        print(f"\n📖 Executing {agent_name} Version 2...")
        user_input_v2 = "Write a short poem about dogs"
        print(f"Prompt: {user_input_v2}")
        response_v2 = openai_client.responses.create(
            input=user_input_v2,
            stream=True,
            extra_body={"agent": {"type": "agent_reference", "name": agent_v2.name, "version": agent_v2.version}}
        )

        print(f"Response: ", end='', flush=True)
        for event in response_v2:
            if event.type == "response.output_text.delta":
                print(event.delta, end='', flush=True)
            elif event.type == "response.completed":
                print()
                break

        # List all versions of the agent
        print(f"\n📋 Listing all versions of agent '{agent_name}'...")
        print(f"{'─'*60}")
        print(f"{'Version':<8} {'Model':<20} {'Instructions'}")
        print(f"{'─'*60}")

        try:
            versions = project_client.agents.list_versions(agent_name)
            for version in versions:
                model_name = getattr(version.definition, 'model', 'Unknown') if hasattr(version, 'definition') else 'Unknown'
                instructions = getattr(version.definition, 'instructions', 'No instructions') if hasattr(version, 'definition') else 'No instructions'

                # Truncate long instructions for display
                if len(instructions) > 40:
                    instructions = instructions[:37] + "..."

                print(f"{version.version:<8} {model_name:<20} {instructions}")
        except Exception as e:
            print(f"❌ Error listing versions: {e}")

        print(f"{'─'*60}")

        # Cleanup based on DELETE flag
        if delete_resources:
            print(f"\n🗑️  Deleting agent versions...")
            try:
                project_client.agents.delete_version(agent_name=agent_name, agent_version="1")
                print(f"✅ Deleted {agent_name}:1")
                project_client.agents.delete_version(agent_name=agent_name, agent_version="2")
                print(f"✅ Deleted {agent_name}:2")
            except Exception as e:
                print(f"❌ Error during cleanup: {e}")
        else:
            print(f"\n💾 Preserved agent versions: {agent_name}:1 and {agent_name}:2")

        print(f"\n{'='*60}")
        print("DEMO COMPLETED")
        print(f"{'='*60}")


if __name__ == '__main__':
    main()
