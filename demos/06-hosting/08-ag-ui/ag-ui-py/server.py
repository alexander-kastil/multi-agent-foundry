import os

import uvicorn
from agent_framework import Agent, Executor, Message, WorkflowBuilder, WorkflowContext, handler, tool
from agent_framework.ag_ui import AgentFrameworkWorkflow, add_agent_framework_fastapi_endpoint
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

load_dotenv(override=True)

TICKETS = {
    "T-1001": "Printer on floor 3 offline, assigned to facilities, priority low",
    "T-1002": "VPN drops every hour for the sales team, assigned to network, priority high",
    "T-1003": "New starter laptop request, assigned to procurement, priority medium",
}


@tool(approval_mode="never_require")
def lookup_ticket(ticket_id: str) -> str:
    """Return the status line for a support ticket id such as T-1001."""
    return TICKETS.get(ticket_id.upper(), f"No ticket {ticket_id} found")


@tool(approval_mode="always_require")
def close_ticket(ticket_id: str, resolution: str) -> str:
    """Close a support ticket with a resolution note."""
    return f"Ticket {ticket_id.upper()} closed: {resolution}"


def create_client() -> FoundryChatClient:
    return FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )


desk_agent = Agent(
    client=create_client(),
    name="support-desk",
    instructions="You are the Contoso support desk agent. Use lookup_ticket to read tickets and close_ticket to close them.",
    tools=[lookup_ticket, close_ticket],
)


class TriageStep(Executor):
    def __init__(self) -> None:
        super().__init__(id="triage")
        self.agent = Agent(
            client=create_client(),
            name="triage",
            instructions="Classify the ticket as network, facilities or procurement and give a priority in one line.",
        )

    @handler
    async def run(self, messages: list[Message], ctx: WorkflowContext[str]) -> None:
        response = await self.agent.run(messages[-1].text)
        await ctx.send_message(response.text)


class ReplyStep(Executor):
    def __init__(self) -> None:
        super().__init__(id="reply")
        self.agent = Agent(
            client=create_client(),
            name="reply",
            instructions="Write a two sentence customer reply that names the team and the priority.",
        )

    @handler
    async def run(self, text: str, ctx: WorkflowContext[str, str]) -> None:
        response = await self.agent.run(text)
        await ctx.yield_output(response.text)


def build_workflow():
    triage = TriageStep()
    reply = ReplyStep()
    return WorkflowBuilder(name="ticket-workflow", start_executor=triage, output_from=[reply]).add_edge(triage, reply).build()


app = FastAPI(title="AG-UI server")
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])

add_agent_framework_fastapi_endpoint(app, desk_agent, "/")
add_agent_framework_fastapi_endpoint(
    app,
    AgentFrameworkWorkflow(workflow_factory=lambda thread_id: build_workflow(), name="ticket-workflow"),
    "/workflow",
)

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=int(os.getenv("AGUI_PORT", "8888")))
