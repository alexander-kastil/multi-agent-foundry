import json
import logging
import os
from datetime import timedelta

import azure.functions as func
from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient
from agent_framework.azure import AgentFunctionApp
from azure.durable_functions import DurableOrchestrationClient, DurableOrchestrationContext
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from pydantic import BaseModel

load_dotenv(override=True)

logger = logging.getLogger(__name__)

WRITER = "AnnouncementWriter"
APPROVAL_EVENT = "ApprovalDecision"


class Announcement(BaseModel):
    title: str
    body: str


class Decision(BaseModel):
    approved: bool
    feedback: str = ""


writer = Agent(
    client=FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    ),
    name=WRITER,
    instructions="You write short product release announcements. Return JSON with title and body.",
)

app = AgentFunctionApp(agents=[writer])


@app.activity_trigger(input_name="announcement")
def request_review(announcement: dict) -> None:
    logger.info("REVIEW REQUESTED: %s", announcement["title"])


@app.activity_trigger(input_name="announcement")
def publish(announcement: dict) -> None:
    logger.info("PUBLISHED: %s", announcement["title"])


@app.orchestration_trigger(context_name="context")
def announcement_approval(context: DurableOrchestrationContext):
    topic = context.get_input()["topic"]
    agent = app.get_agent(context, WRITER)
    session = agent.create_session()

    draft = yield agent.run(
        messages=f"Write a release announcement about {topic}.",
        session=session,
        options={"response_format": Announcement},
    )
    announcement = draft.value

    for attempt in range(3):
        context.set_custom_status(f"waiting for review, round {attempt + 1}")
        yield context.call_activity("request_review", announcement.model_dump())

        approval = context.wait_for_external_event(APPROVAL_EVENT)
        timeout = context.create_timer(context.current_utc_datetime + timedelta(hours=72))
        winner = yield context.task_any([approval, timeout])
        if winner != approval:
            raise TimeoutError("No review decision within 72 hours")
        timeout.cancel()

        decision = Decision.model_validate_json(approval.result)
        if decision.approved:
            yield context.call_activity("publish", announcement.model_dump())
            context.set_custom_status("published")
            return announcement.model_dump()

        revised = yield agent.run(
            messages=f"The reviewer rejected it. Rewrite it with this feedback: {decision.feedback}",
            session=session,
            options={"response_format": Announcement},
        )
        announcement = revised.value

    raise RuntimeError("Announcement was not approved after 3 rounds")


@app.route(route="approvals/run", methods=["POST"])
@app.durable_client_input(client_name="client")
async def start(req: func.HttpRequest, client: DurableOrchestrationClient) -> func.HttpResponse:
    instance_id = await client.start_new("announcement_approval", client_input=req.get_json())
    return func.HttpResponse(json.dumps({"instanceId": instance_id}), status_code=202, mimetype="application/json")


@app.route(route="approvals/decision/{instanceId}", methods=["POST"])
@app.durable_client_input(client_name="client")
async def decide(req: func.HttpRequest, client: DurableOrchestrationClient) -> func.HttpResponse:
    decision = Decision.model_validate(req.get_json())
    await client.raise_event(req.route_params["instanceId"], APPROVAL_EVENT, decision.model_dump())
    return func.HttpResponse(json.dumps({"sent": decision.approved}), mimetype="application/json")


@app.route(route="approvals/status/{instanceId}", methods=["GET"])
@app.durable_client_input(client_name="client")
async def status(req: func.HttpRequest, client: DurableOrchestrationClient) -> func.HttpResponse:
    state = await client.get_status(req.route_params["instanceId"])
    body = {"runtimeStatus": state.runtime_status.name, "workflowStatus": state.custom_status, "output": state.output}
    return func.HttpResponse(json.dumps(body), mimetype="application/json")
