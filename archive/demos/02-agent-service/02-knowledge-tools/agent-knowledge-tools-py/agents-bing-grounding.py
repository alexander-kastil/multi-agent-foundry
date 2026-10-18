import os
import io
import sys
from dotenv import load_dotenv
from azure.ai.agents import AgentsClient
from azure.ai.projects import AIProjectClient
from azure.ai.agents.models import MessageRole, BingGroundingTool, ListSortOrder
from azure.identity import DefaultAzureCredential

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

def main():
    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    bing_connection_name = os.getenv("BING_CONNECTION")

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Using Bing connection: {bing_connection_name}")

    project_client = AIProjectClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )

    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )

    # [START create_agent_with_bing_grounding_tool]
    conn_id = project_client.connections.get(bing_connection_name).id
    print(f"Connection ID: {conn_id}")

    # Initialize agent bing tool and add the connection id
    bing = BingGroundingTool(connection_id=conn_id)

    # Create agent with the bing tool and process agent run
    with agents_client:
        agent = agents_client.create_agent(
            model=model,
            name="bing-grounding-agent",
            instructions="You are a helpful agent",
            description="Demonstrates Bing Grounding tool to search the web and provide current, factual information with citations.",
            tools=bing.definitions,
        )
        # [END create_agent_with_bing_grounding_tool]

        print(f"Created agent, agent ID: {agent.id}")

        # Create thread for communication
        thread = agents_client.threads.create()
        print(f"Created thread, thread ID: {thread.id}")

        # Create message to thread
        message = agents_client.messages.create(
            thread_id=thread.id,
            role=MessageRole.USER,
            content="How does wikipedia explain Euler's Identity?",
        )
        print(f"Created message, message ID: {message.id}")

        # Create and process agent run in thread with tools
        run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
        print(f"Run finished with status: {run.status}")

        if run.status == "failed":
            print(f"Run failed: {run.last_error}")

        # Fetch run steps to get the details of the agent run
        run_steps = agents_client.run_steps.list(thread_id=thread.id, run_id=run.id)
        for step in run_steps:
            print(f"Step {step['id']} status: {step['status']}")
            step_details = step.get("step_details", {})
            tool_calls = step_details.get("tool_calls", [])

            if tool_calls:
                print("  Tool calls:")
                for call in tool_calls:
                    print(f"    Tool Call ID: {call.get('id')}")
                    print(f"    Type: {call.get('type')}")

                    bing_grounding_details = call.get("bing_grounding", {})
                    if bing_grounding_details:
                        print(f"    Bing Grounding ID: {bing_grounding_details.get('requesturl')}")

            print()  # add an extra newline between steps

        # Delete the agent when done (if configured)
        delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
        if delete_on_exit:
            agents_client.delete_agent(agent.id)
            print("Deleted agent")
        else:
            print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")

        # Print the Agent's response message with optional citation
        messages = agents_client.messages.list(thread_id=thread.id, order=ListSortOrder.DESCENDING)
        for message in messages:
            if message.role == MessageRole.AGENT:
                responses = []
                for text_message in message.text_messages:
                    responses.append(text_message.text.value)
                msg_text = " ".join(responses)
                for annotation in message.url_citation_annotations:
                    msg_text = msg_text.replace(
                        annotation.text, f" [{annotation.url_citation.title}]({annotation.url_citation.url})"
                    )
                print(f"Agent response: {msg_text}")
                break


if __name__ == '__main__':
    main()
