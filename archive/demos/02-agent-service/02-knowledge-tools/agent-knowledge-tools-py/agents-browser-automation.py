import os
import io
import sys
import logging
from dotenv import load_dotenv
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import (
    MessageRole,
    RunStepToolCallDetails,
    BrowserAutomationTool,
    RunStepBrowserAutomationToolCall,
    ListSortOrder,
)
from azure.identity import DefaultAzureCredential

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

def main():
    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    connection_id = os.getenv("AZURE_PLAYWRIGHT_CONNECTION_ID")
    onvista_url = os.getenv("ONVISTA_URL", "https://www.onvista.de/rohstoffe/Goldpreis-1472977")
    detailed_logging = os.getenv("DETAILED_LOGGING", "false").lower() == "true"
    
    # Adjust logging level based on DETAILED_LOGGING setting
    if not detailed_logging:
        logging.getLogger().setLevel(logging.WARNING)
        # Suppress Azure SDK debug logs
        logging.getLogger('azure').setLevel(logging.WARNING)
        logging.getLogger('urllib3').setLevel(logging.WARNING)

    print(f"\n{'='*80}")
    print(f"Configuration:")
    print(f"{'='*80}")
    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Using connection_id: {connection_id}")
    print(f"Using URL: {onvista_url}")
    print(f"Detailed logging: {detailed_logging}")
    print(f"{'='*80}\n")

    if detailed_logging:
        logging.info("Initializing AgentsClient...")
    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )
    if detailed_logging:
        logging.info("AgentsClient initialized successfully")
    
    # Track created resources for cleanup
    agent = None
    thread = None

    # [START create_agent_with_browser_automation]
    # Initialize Browser Automation tool and add the connection id
    if detailed_logging:
        logging.info(f"Creating BrowserAutomationTool with connection_id: {connection_id}")
    browser_automation = BrowserAutomationTool(connection_id=connection_id)
    if detailed_logging:
        logging.info("BrowserAutomationTool created successfully")

    try:
        with agents_client:
            # Create a new Agent that has the Browser Automation tool attached.
            # Note: To add Browser Automation tool to an existing Agent with an `agent_id`, do the following:
            # agent = agents_client.update_agent(agent_id, tools=browser_automation.definitions)
            if detailed_logging:
                logging.info("Creating agent with browser automation tool...")
            agent = agents_client.create_agent(
                model=model,
                name="browser-automation-agent",
                instructions=f"""
            You are an expert on stock and metals prices. 
            You can retrieve current price information by navigating to {onvista_url} using the Browser Automation tool.
            When asked for a price, navigate to the website and extract the current price information.
            """,
                description="Expert agent for retrieving stock and metals prices from onvista.de using browser automation.",
                tools=browser_automation.definitions,
            )

            # [END create_agent_with_browser_automation]

            print(f"\n✓ Created agent, agent ID: {agent.id}")
            if detailed_logging:
                logging.info(f"Agent created: {agent.id}")

            # Create thread for communication
            if detailed_logging:
                logging.info("Creating thread...")
            thread = agents_client.threads.create()
            print(f"✓ Created thread, thread ID: {thread.id}")
            if detailed_logging:
                logging.info(f"Thread created: {thread.id}")

            # Create message to thread
            if detailed_logging:
                logging.info("Creating message...")
            message = agents_client.messages.create(
                thread_id=thread.id,
                role=MessageRole.USER,
                content="Get the current price of Gold",
            )
            print(f"✓ Created message, message ID: {message.id}")
            if detailed_logging:
                logging.info(f"Message created: {message.id}")

            # Create and process agent run in thread with tools
            print(f"\n{'='*80}")
            print(f"Running agent... (this may take a while)")
            print(f"{'='*80}")
            print(f"\n⏳ Browser automation in progress...", flush=True)
            print(f"   This can take up to 60-90 seconds as the agent:", flush=True)
            print(f"   - Launches a browser session in Azure Playwright", flush=True)
            print(f"   - Navigates to the website", flush=True)
            print(f"   - Extracts the requested information", flush=True)
            print(f"   Please wait...\n", flush=True)
            if detailed_logging:
                logging.info("Starting agent run...")
            run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
            print(f"\n✓ Run finished with status: {run.status}")
            if detailed_logging:
                logging.info(f"Run completed with status: {run.status}")

            if run.status == "failed":
                print(f"\n❌ Run failed: {run.last_error}")
                logging.error(f"Run failed: {run.last_error}")

            # Fetch run steps to get the details of the agent run
            if detailed_logging:
                logging.info("Fetching run steps...")
            run_steps = agents_client.run_steps.list(thread_id=thread.id, run_id=run.id)
            
            print(f"\n{'='*80}")
            print(f"Run Steps Details:")
            print(f"{'='*80}\n")
            
            for step in run_steps:
                print(f"Step {step.id} status: {step.status}")
                if detailed_logging:
                    logging.info(f"Processing step {step.id} with status {step.status}")

                if isinstance(step.step_details, RunStepToolCallDetails):
                    print("  Tool calls:")
                    tool_calls = step.step_details.tool_calls
                    if detailed_logging:
                        logging.info(f"  Found {len(tool_calls)} tool call(s)")

                    for call in tool_calls:
                        print(f"    Tool call ID: {call.id}")
                        print(f"    Tool call type: {call.type}")
                        if detailed_logging:
                            logging.debug(f"    Processing tool call {call.id} of type {call.type}")

                        if isinstance(call, RunStepBrowserAutomationToolCall):
                            print(f"    Browser automation input: {call.browser_automation.input}")
                            print(f"    Browser automation output: {call.browser_automation.output}")
                            if detailed_logging:
                                logging.debug(f"    Browser automation input: {call.browser_automation.input}")
                                logging.debug(f"    Browser automation output: {call.browser_automation.output}")

                            print("    Steps:")
                            for tool_step in call.browser_automation.steps:
                                print(f"      Last step result: {tool_step.last_step_result}")
                                print(f"      Current state: {tool_step.current_state}")
                                print(f"      Next step: {tool_step.next_step}")
                                if detailed_logging:
                                    logging.debug(f"      Step - Result: {tool_step.last_step_result}")
                                print()  # add an extra newline between tool steps

                        print()  # add an extra newline between tool calls

                print()  # add an extra newline between run steps

            # Optional: Delete the agent once the run is finished.
            # Controlled by DELETE_AGENT_ON_EXIT environment variable
            delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
            if delete_on_exit and agent:
                if detailed_logging:
                    logging.info(f"Deleting agent {agent.id}...")
                agents_client.delete_agent(agent.id)
                print("✓ Deleted agent")
            else:
                print(f"✓ Agent {agent.id} preserved for examination in Azure AI Foundry")
                if detailed_logging:
                    logging.info(f"Agent {agent.id} preserved")

            # Print the Agent's response message with optional citation
            print(f"\n{'='*80}")
            print(f"Agent Response:")
            print(f"{'='*80}\n")
            
            if detailed_logging:
                logging.info("Fetching messages...")
            messages = agents_client.messages.list(thread_id=thread.id, order=ListSortOrder.DESCENDING)
            for message in messages:
                if message.role == MessageRole.AGENT:
                    for text_message in message.text_messages:
                        print(f"Agent response: {text_message.text.value}")
                        if detailed_logging:
                            logging.info(f"Agent response: {text_message.text.value}")
                    for annotation in message.url_citation_annotations:
                        print(f"URL Citation: [{annotation.url_citation.title}]({annotation.url_citation.url})")
                        if detailed_logging:
                            logging.info(f"URL Citation: [{annotation.url_citation.title}]({annotation.url_citation.url})")
                    break
            
            print(f"\n{'='*80}")
            print(f"Completed successfully!")
            print(f"{'='*80}\n")
    
    except KeyboardInterrupt:
        print(f"\n\n{'='*80}")
        print("❌ Interrupted by user (Ctrl+C)")
        print(f"{'='*80}")
        if detailed_logging:
            logging.warning("Process interrupted by user")
        
        # Clean up resources if they were created
        try:
            if agent:
                delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
                if delete_on_exit:
                    print(f"Cleaning up agent {agent.id}...")
                    agents_client.delete_agent(agent.id)
                    print("✓ Agent deleted")
                else:
                    print(f"⚠ Agent {agent.id} preserved for examination in Azure AI Foundry")
        except Exception as cleanup_error:
            if detailed_logging:
                logging.error(f"Error during cleanup: {cleanup_error}")
            print(f"⚠ Could not clean up resources: {cleanup_error}")
    
    except Exception as e:
        print(f"\n\n{'='*80}")
        print(f"❌ Error occurred: {type(e).__name__}")
        print(f"   {str(e)}")
        print(f"{'='*80}")
        if detailed_logging:
            logging.error(f"Error occurred: {e}", exc_info=True)
        
        # Clean up resources if they were created
        try:
            if agent:
                delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
                if delete_on_exit:
                    print(f"Cleaning up agent {agent.id}...")
                    agents_client.delete_agent(agent.id)
                    print("✓ Agent deleted")
        except Exception as cleanup_error:
            if detailed_logging:
                logging.error(f"Error during cleanup: {cleanup_error}")


if __name__ == '__main__':
    main()
