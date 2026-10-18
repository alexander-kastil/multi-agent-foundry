import os
import time
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import FileSearchToolDefinition, FileSearchToolResource, ToolResources

def main():

    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    vector_store_id = os.getenv("VECTOR_STORE_ID")
    delete_resources = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Using vector store: {vector_store_id}")
    print(f"Delete resources: {delete_resources}")

    # Connect to Microsoft Foundry using legacy AgentsClient
    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential()
    )

    with agents_client:
        start = time.time()

        # Create agent with file search tool
        agent = agents_client.create_agent(
            model=model,
            name="file-search-agent",
            instructions="You are a helpful agent that can search through documents to answer questions. Use the file search tool to find relevant information.",
            tools=[FileSearchToolDefinition()],
            tool_resources=ToolResources(
                file_search=FileSearchToolResource(
                    vector_store_ids=[vector_store_id]
                )
            )
        )
        print(f"Created agent: {agent.name}, ID: {agent.id}")
        print(f"✓ Agent is now visible in the Microsoft Foundry portal under 'Agents'")
        print(f"  View at: {endpoint.replace('/api/projects/', '/projects/')}")

        # Create a thread for the conversation
        thread = agents_client.threads.create()
        print(f"Created thread, thread ID: {thread.id}")

        # Create message in thread
        message = agents_client.messages.create(
            thread_id=thread.id,
            role="user",
            content="Tell me about Equinox Gold"
        )
        print(f"Created message, message ID: {message.id}")

        # Create and poll run
        run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
        print(f"Run completed with status: {run.status}")

        duration = time.time() - start
        print(f"Run took {duration:.2f}s")

        if run.status == "failed":
            print(f"Run error: {run.last_error}")

        # List messages
        messages = agents_client.messages.list(thread_id=thread.id)
        
        print("\n--- Conversation ---")
        for data_point in reversed(list(messages)):
            last_message_content = data_point.content[-1]
            if isinstance(last_message_content, dict):
                print(f"{data_point.role}: {last_message_content}")
            else:
                print(f"{data_point.role}: {last_message_content.text.value}")
        
        # Cleanup based on DELETE flag
        if delete_resources:
            agents_client.delete_agent(agent.id)
            print("\nDeleted agent")
        else:
            print(f"\nPreserved agent: {agent.id}")

if __name__ == '__main__':
    main()
