import os
from typing import Any

from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from agent_framework_hosting import AgentState
from agent_framework_hosting_responses import (
    create_response_id,
    responses_from_run,
    responses_session_id,
    responses_to_run,
)
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from fastapi import Body, FastAPI, HTTPException
from fastapi.responses import JSONResponse

load_dotenv(override=True)

TICKETS = {
    "T-1001": "Printer on floor 3 offline, assigned to facilities, priority low",
    "T-1002": "VPN drops every hour for the sales team, assigned to network, priority high",
    "T-1003": "New starter laptop request, assigned to procurement, priority medium",
}

ALLOWED_REQUEST_OPTIONS = frozenset({"max_tokens"})


@tool
def lookup_ticket(ticket_id: str) -> str:
    """Return the status line for a support ticket id such as T-1001."""
    return TICKETS.get(ticket_id.upper(), f"No ticket {ticket_id} found")


def create_agent() -> Agent:
    return Agent(
        client=FoundryChatClient(
            project_endpoint=os.environ["PROJECT_ENDPOINT"],
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=DefaultAzureCredential(),
        ),
        name="support-desk",
        instructions="You are the Contoso support desk agent. Use lookup_ticket for ticket questions and answer in two sentences.",
        tools=[lookup_ticket],
        default_options={"store": False},
    )


app = FastAPI()
state = AgentState(create_agent)


@app.post("/responses", response_model=None)
async def responses(body: dict[str, Any] = Body(...)) -> JSONResponse:
    try:
        run = responses_to_run(body)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    session_id, is_conversation_id = responses_session_id(body)
    conversation_id = session_id if is_conversation_id else None
    response_id = create_response_id()
    options = {key: value for key, value in run["options"].items() if key in ALLOWED_REQUEST_OPTIONS}

    target = await state.get_target()
    session = await state.get_or_create_session(session_id or response_id)
    result = await target.run(run["messages"], session=session, options=options)
    await state.set_session(conversation_id or response_id, session)

    return JSONResponse(responses_from_run(result, response_id=response_id, conversation_id=conversation_id))
