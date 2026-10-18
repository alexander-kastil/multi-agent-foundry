import os

from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from agent_framework_foundry_hosting import ResponsesHostServer
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

TICKETS = {
    "T-1001": "Printer on floor 3 offline, assigned to facilities, priority low",
    "T-1002": "VPN drops every hour for the sales team, assigned to network, priority high",
    "T-1003": "New starter laptop request, assigned to procurement, priority medium",
}

INSTRUCTIONS = (
    "You are the Contoso support desk agent. "
    "Use lookup_ticket to answer questions about tickets and runtime_info to answer questions about your hosting. "
    "Keep answers to two sentences."
)

INJECTED = (
    "FOUNDRY_PROJECT_ENDPOINT",
    "AZURE_AI_MODEL_DEPLOYMENT_NAME",
    "APPLICATIONINSIGHTS_CONNECTION_STRING",
    "FOUNDRY_AGENT_NAME",
    "FOUNDRY_AGENT_VERSION",
)


@tool
def lookup_ticket(ticket_id: str) -> str:
    """Return the status line for a support ticket id such as T-1001."""
    return TICKETS.get(ticket_id.upper(), f"No ticket {ticket_id} found")


@tool
def runtime_info() -> str:
    """Report which platform variables are injected into this container and the agent name and version."""
    present = [name for name in INJECTED if os.getenv(name)]
    return f"injected={present}; agent={os.getenv('FOUNDRY_AGENT_NAME')} version={os.getenv('FOUNDRY_AGENT_VERSION')}"


client = FoundryChatClient(
    project_endpoint=os.getenv("FOUNDRY_PROJECT_ENDPOINT") or os.environ["PROJECT_ENDPOINT"],
    model=os.getenv("AZURE_AI_MODEL_DEPLOYMENT_NAME") or os.environ["MODEL_DEPLOYMENT_NAME"],
    credential=DefaultAzureCredential(),
)

agent = Agent(client=client, name="m6-support-desk", instructions=INSTRUCTIONS, tools=[lookup_ticket, runtime_info])

if __name__ == "__main__":
    ResponsesHostServer(agent).run()
