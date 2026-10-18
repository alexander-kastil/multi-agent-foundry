import os
from dataclasses import dataclass
from typing import Any

from agent_framework import (
    AgentExecutorRequest,
    AgentExecutorResponse,
    Executor,
    FileCheckpointStorage,
    Message,
    WorkflowBuilder,
    WorkflowContext,
    executor,
    handler,
    response_handler,
)
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from typing_extensions import Never

CHECKPOINT_DIR = "checkpoints"

SUMMARIZER_INSTRUCTIONS = (
    "You prepare expense reports for approval. Reply with two sentences: what was bought and "
    "whether the amount looks normal for it."
)


@dataclass
class ApprovalRequest:
    summary: str


@executor(id="intake")
async def intake(report: str, ctx: WorkflowContext[AgentExecutorRequest]) -> None:
    await ctx.send_message(AgentExecutorRequest(messages=[Message("user", [report])], should_respond=True))


class ApprovalGate(Executor):
    def __init__(self) -> None:
        super().__init__(id="approval_gate")
        self.decisions = 0

    @handler
    async def ask(self, response: AgentExecutorResponse, ctx: WorkflowContext) -> None:
        await ctx.request_info(
            request_data=ApprovalRequest(summary=response.agent_response.text),
            response_type=str,
        )

    @response_handler
    async def decide(self, original_request: ApprovalRequest, decision: str, ctx: WorkflowContext[Never, str]) -> None:
        self.decisions += 1
        await ctx.yield_output(f"decision {self.decisions}: {decision.upper()} for: {original_request.summary}")

    async def on_checkpoint_save(self) -> dict[str, Any]:
        return {"decisions": self.decisions}

    async def on_checkpoint_restore(self, state: dict[str, Any]) -> None:
        self.decisions = state.get("decisions", 0)


def build_workflow() -> tuple:
    storage = FileCheckpointStorage(CHECKPOINT_DIR, allowed_checkpoint_types=["expense:ApprovalRequest"])
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    summarizer = client.as_agent(name="expense_summarizer", instructions=SUMMARIZER_INSTRUCTIONS)
    gate = ApprovalGate()
    workflow = (
        WorkflowBuilder(
            name="expense_approval",
            start_executor=intake,
            output_from=[gate],
            checkpoint_storage=storage,
        )
        .add_edge(intake, summarizer)
        .add_edge(summarizer, gate)
        .build()
    )
    return workflow, storage
