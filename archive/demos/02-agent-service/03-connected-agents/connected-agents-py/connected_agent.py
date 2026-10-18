import os
import logging
from dotenv import load_dotenv
from datetime import datetime
import io
import sys

# Add references
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import ConnectedAgentTool, MessageRole, ListSortOrder, ToolSet, FunctionTool
from azure.identity import DefaultAzureCredential

# Import logging configuration
from log_util import LogUtil, vdebug

# Import diagram generator
from diagram_generator import MermaidDiagramGenerator

"""Connected Agents triage demo using Azure AI AgentsClient.
Applies migration guidance from upgrade.md: standalone AgentsClient, updated env handling,
Windows UTF-8 console configuration, and ticket folder nested under output path.
"""

# Load environment variables early
load_dotenv()

# Configure UTF-8 encoding for Windows console (emoji/symbol safety)
try:
    if sys.stdout and hasattr(sys.stdout, "buffer"):
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
except Exception:
    pass  # Non-critical; continue if wrapping fails

# Configuration constants

# Read logging configuration from environment
verbose_output = os.getenv("VERBOSE_OUTPUT", "false") == "true"
create_mermaid_diagram = os.getenv("CREATE_MERMAID_DIAGRAM", "false") == "true"

# Derive ticket folder path nested under output path per requirement
output_path = os.getenv("OUTPUT_PATH", "./output")
ticket_folder_name = os.getenv("TICKET_FOLDER", "tickets")
ticket_folder = os.path.join(output_path, ticket_folder_name)

# Ensure base output directory exists early (ticket subfolder created later by generator)
os.makedirs(output_path, exist_ok=True)

# Setup logging with explicit parameters
logging_config = LogUtil()
logging_config.setup_logging(verbose=verbose_output)

# Read required settings
project_endpoint = os.getenv("PROJECT_ENDPOINT")
model_deployment = os.getenv("MODEL_DEPLOYMENT")

if not project_endpoint or not model_deployment:
    logging.warning("Environment variables PROJECT_ENDPOINT or MODEL_DEPLOYMENT are missing. Script may fail to connect.")
else:
    logging.info(f"Using project endpoint: {project_endpoint}")
    logging.info(f"Using model deployment: {model_deployment}")

# Priority agent definition
priority_agent_name = "priority_agent"
priority_agent_instructions = """
Assess how urgent a ticket is based on its description.

Respond with one of the following levels:
- High: User-facing or blocking issues
- Medium: Time-sensitive but not breaking anything
- Low: Cosmetic or non-urgent tasks

Only output the urgency level and a very brief explanation.
"""

# Team agent definition
team_agent_name = "connected_supervisor_agent"
team_agent_instructions = """
Decide which team should own each ticket.

Choose from the following teams:
- Frontend
- Backend
- Infrastructure
- Marketing

Base your answer on the content of the ticket. Respond with the team name and a very brief explanation.
"""

# Effort agent definition
effort_agent_name = "effort_agent"
effort_agent_instructions = """
Estimate how much work each ticket will require.

Use the following scale:
- Small: Can be completed in a day
- Medium: 2-3 days of work
- Large: Multi-day or cross-team effort

Base your estimate on the complexity implied by the ticket. Respond with the effort level and a brief justification.
"""

# Instructions for the orchestrator agent
orchestrator_agent_instructions = """
Triage the given ticket. Use the connected tools to determine the ticket's priority, 
which team it should be assigned to, and how much effort it may take.
"""

# Connect to the agents client
logging.info("Initializing AgentsClient ...")
agents_client = AgentsClient(
    endpoint=project_endpoint,
    credential=DefaultAzureCredential(
        exclude_environment_credential=True,
        exclude_managed_identity_credential=True
    ),
)
logging.info("AgentsClient initialized.")

with agents_client:

    # Create the priority agent on the Azure AI agent service
    logging.info("Creating priority agent ...")
    priority_agent = agents_client.create_agent(
        model=model_deployment,
        name=priority_agent_name,
        instructions=priority_agent_instructions
    )
    logging.info(f"Priority agent created: id={priority_agent.id}")

    # Create a connected agent tool for the priority agent
    priority_agent_tool = ConnectedAgentTool(
        id=priority_agent.id, 
        name=priority_agent_name, 
        description="Assess the priority of a ticket"
    )

    # Create the team agent and connected tool
    logging.info("Creating team agent ...")
    team_agent = agents_client.create_agent(
        model=model_deployment,
        name=team_agent_name,
        instructions=team_agent_instructions
    )
    logging.info(f"Team agent created: id={team_agent.id}")
    
    team_agent_tool = ConnectedAgentTool(
        id=team_agent.id, 
        name=team_agent_name, 
        description="Determines which team should take the ticket"
    )
    
    # Create the effort agent and connected tool
    logging.info("Creating effort agent ...")
    effort_agent = agents_client.create_agent(
        model=model_deployment,
        name=effort_agent_name,
        instructions=effort_agent_instructions
    )
    logging.info(f"Effort agent created: id={effort_agent.id}")
    
    effort_agent_tool = ConnectedAgentTool(
        id=effort_agent.id, 
        name=effort_agent_name, 
        description="Determines the effort required to complete the ticket"
    )

    # Create a main agent with the Connected Agent tools
    logging.info("Creating triage agent with connected tools ...")
    agent = agents_client.create_agent(
        model=model_deployment,
        name="orchestrator-agent",
        instructions=orchestrator_agent_instructions,
        tools=[
            priority_agent_tool.definitions[0],
            team_agent_tool.definitions[0],
            effort_agent_tool.definitions[0]
        ]
    )
    logging.info(f"Triage agent created: id={agent.id}")
    logging.debug(f"Tool definitions: {[t for t in agent.tools]}")
    
    # Create thread for the chat session
    logging.info("Creating a new thread for the triage session ...")
    thread = agents_client.threads.create()
    logging.info(f"Thread created: id={thread.id}")

    # Create the ticket prompt
    prompt = "Users can't reset their password from the mobile app."
    logging.info(f"Prompt prepared: {prompt}")

    # Send a prompt to the agent
    logging.info("Sending user message to thread ...")
    message = agents_client.messages.create(
        thread_id=thread.id,
        role=MessageRole.USER,
        content=prompt,
    )
    logging.info(f"Message sent: id={message.id}, role={message.role}")
    
    # Create and process Agent run in thread with tools
    logging.info("Starting run (create_and_process) ...")
    run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
    logging.info(f"Run finished: id={run.id}, status={run.status}")
    logging.debug(f"Run raw object: {run}")
    
    if run.status == "failed":
        logging.error(f"Run failed: {run.last_error}")
    else:
        logging.info("Run succeeded. Collecting messages ...")

    # Fetch and log all messages
    messages = agents_client.messages.list(thread_id=thread.id, order=ListSortOrder.ASCENDING)
    resolution = ""
    for message in messages:
        if message.text_messages:
            last_msg = message.text_messages[-1]
            # Show role and content succinctly
            logging.info(f"Message ({message.role}): {last_msg.text.value.strip()}")
            logging.debug(f"Full message object: {message}")
            # Capture the assistant's final response as resolution
            if message.role == MessageRole.AGENT:
                resolution = last_msg.text.value.strip()
    
    # Extract token usage from run
    token_usage_in = getattr(run.usage, 'prompt_tokens', 0) if hasattr(run, 'usage') else 0
    token_usage_out = getattr(run.usage, 'completion_tokens', 0) if hasattr(run, 'usage') else 0
    
    # Generate diagram if enabled
    if create_mermaid_diagram:
        logging.info("Generating Mermaid diagram ...")
        diagram_generator = MermaidDiagramGenerator(ticket_folder_path=ticket_folder)
        diagram_generator.save_diagram_file(
            ticket_prompt=prompt,
            resolution=resolution,
            token_usage_in=token_usage_in,
            token_usage_out=token_usage_out
        )
    
    # Delete the agent when done
    logging.info("Cleaning up agents ...")
    agents_client.delete_agent(agent.id)
    logging.info("Deleted triage agent.")

    # Delete the connected agents when done
    agents_client.delete_agent(priority_agent.id)
    logging.info("Deleted priority agent.")
    agents_client.delete_agent(team_agent.id)
    logging.info("Deleted team agent.")
    agents_client.delete_agent(effort_agent.id)
    logging.info("Deleted effort agent.")
