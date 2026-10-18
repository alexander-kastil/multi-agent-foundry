import os
import io
import sys
from dotenv import load_dotenv
from azure.ai.agents import AgentsClient
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from azure.ai.agents.models import ListSortOrder, MessageRole, SharepointTool

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def log(message: str) -> None:
    """Print log message if detailed logging is enabled."""
    detailed_logging = os.getenv("DETAILED_LOGGING", "false").lower() == "true"
    if detailed_logging:
        print(f"[LOG] {message}")


def main():
    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    sharepoint_connection_name = os.getenv("SHAREPOINT_CONNECTION")

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"SharePoint connection: {sharepoint_connection_name}")
    log(f"Detailed logging enabled")
    log(f"Environment loaded: endpoint={endpoint}, model={model}, connection={sharepoint_connection_name}")

    project_client = AIProjectClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )
    log("Created AIProjectClient")

    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )
    log("Created AgentsClient")

    conn_id = project_client.connections.get(sharepoint_connection_name).id
    print(f"Connection ID: {conn_id}")
    log(f"Retrieved SharePoint connection ID: {conn_id}")

    # Initialize Sharepoint tool with connection id
    sharepoint = SharepointTool(connection_id=conn_id)
    log(f"Created SharepointTool with connection_id: {conn_id}")
    log(f"SharePoint tool definitions: {sharepoint.definitions}")

    # Create agent with Sharepoint tool and process agent run
    with agents_client:
        agent = agents_client.create_agent(
            model=model,
            name="sharepoint-agent",
            instructions=(
                "You are a SharePoint knowledge assistant. "
                "You MUST search SharePoint for relevant documents before answering any question. "
                "Always use the SharePoint tool to find information from the connected SharePoint site. "
                "Include citations from the SharePoint documents in your responses. "
                "Do not rely on general knowledge - only answer based on SharePoint content."
            ),
            description="Demonstrates SharePoint integration to search and retrieve documents from SharePoint sites with citation support.",
            tools=sharepoint.definitions,
        )
        print(f"Created agent, agent ID: {agent.id}")
        log(f"Agent created with model={model}, name={agent.name}, tools={len(sharepoint.definitions)} SharePoint tools")

        # Create thread for communication
        thread = agents_client.threads.create()
        print(f"Created thread, thread ID: {thread.id}")
        log(f"Thread created: {thread.id}")

        # Create message to thread
        message = agents_client.messages.create(
            thread_id=thread.id,
            role="user",
            content="What kind of sightseeing can you recommend in Vienna?",
        )
        print(f"Created message, message ID: {message.id}")
        log(f"User message: 'What kind of sightseeing can you recommend in Vienna?'")

        # Create and process agent run in thread with tools
        log("Starting run...")
        run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
        print(f"Run finished with status: {run.status}")
        log(f"Run status: {run.status}")
        
        # Log run steps to see if SharePoint tool was invoked
        if hasattr(run, 'id'):
            try:
                steps = agents_client.runs.steps.list(thread_id=thread.id, run_id=run.id)
                log(f"Run steps count: {len(steps) if hasattr(steps, '__len__') else 'unknown'}")
                for idx, step in enumerate(steps):
                    log(f"Step {idx + 1}: type={step.type}, status={step.status}")
                    if hasattr(step, 'step_details'):
                        log(f"  Step details type: {type(step.step_details).__name__}")
                        if hasattr(step.step_details, 'tool_calls'):
                            for tool_idx, tool_call in enumerate(step.step_details.tool_calls):
                                log(f"  Tool call {tool_idx + 1}: {tool_call.type if hasattr(tool_call, 'type') else type(tool_call).__name__}")
                                if hasattr(tool_call, 'sharepoint_grounding'):
                                    log(f"    SharePoint grounding used!")
            except Exception as e:
                log(f"Could not retrieve run steps: {e}")

        if run.status == "failed":
            # Check if you got "Rate limit is exceeded.", then you want to get more quota
            print(f"Run failed: {run.last_error}")
            log(f"Run failure details: {run.last_error}")

        # Delete the agent when done (if configured)
        delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
        if delete_on_exit:
            agents_client.delete_agent(agent.id)
            print("Deleted agent")
        else:
            print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")
        
        print(f"\nYou can examine my thread using this id: {thread.id}")

        # Fetch and log all messages
        messages = agents_client.messages.list(thread_id=thread.id, order=ListSortOrder.ASCENDING)
        log(f"Retrieved {len(messages) if hasattr(messages, '__len__') else 'unknown'} messages")
        print("\nConversation:")
        print("-" * 50)
        for msg in messages:
            log(f"Message role: {msg.role}, ID: {msg.id}")
            if msg.text_messages:
                log(f"  Text messages count: {len(msg.text_messages)}")
                responses = []
                for text_message in msg.text_messages:
                    responses.append(text_message.text.value)
                message_text = " ".join(responses)
                
                # Log citations if present
                if msg.url_citation_annotations:
                    log(f"  URL citations found: {len(msg.url_citation_annotations)}")
                    for idx, annotation in enumerate(msg.url_citation_annotations):
                        log(f"    Citation {idx + 1}: title={annotation.url_citation.title}, url={annotation.url_citation.url}")
                        message_text = message_text.replace(
                            annotation.text, f" [{annotation.url_citation.title}]({annotation.url_citation.url})"
                        )
                else:
                    log(f"  No URL citations found (SharePoint may not have been used)")
                    
                print(f"{msg.role}: {message_text}")
                print("-" * 50)


if __name__ == '__main__':
    main()
