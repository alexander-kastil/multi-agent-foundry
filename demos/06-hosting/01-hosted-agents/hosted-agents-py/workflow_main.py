import os

from agent_framework import Agent, Executor, Workflow, WorkflowBuilder, WorkflowContext, handler
from agent_framework.foundry import FoundryChatClient
from agent_framework_foundry_hosting import (
    CheckpointStoreProvider,
    HostedResponseRequest,
    ResponsesHostServer,
    WorkflowTurn,
)
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from pydantic import BaseModel, Field

load_dotenv(override=True)


class Ticket(BaseModel):
    ticket_id: str = Field(min_length=1)
    question: str = Field(min_length=1)


class DeskAgent(Agent):
    def __init__(self, name: str, instructions: str) -> None:
        self.credential = DefaultAzureCredential()
        client = FoundryChatClient(
            project_endpoint=os.getenv("FOUNDRY_PROJECT_ENDPOINT") or os.environ["PROJECT_ENDPOINT"],
            model=os.getenv("AZURE_AI_MODEL_DEPLOYMENT_NAME") or os.environ["MODEL_DEPLOYMENT_NAME"],
            credential=self.credential,
        )
        self.project_client = client.project_client
        super().__init__(client=client, name=name, instructions=instructions, default_options={"store": False})

    async def close(self) -> None:
        try:
            await super().close()
        finally:
            await self.project_client.close()
            self.credential.close()


class IntakeExecutor(Executor):
    def __init__(self) -> None:
        super().__init__(id="intake")

    @handler
    async def intake(self, ticket: Ticket, ctx: WorkflowContext[str]) -> None:
        await ctx.send_message(f"Ticket {ticket.ticket_id}: {ticket.question}")


class AgentStepExecutor(Executor):
    def __init__(self, agent: DeskAgent, final: bool = False) -> None:
        super().__init__(id=agent.name)
        self.agent = agent
        self.final = final

    @handler
    async def step(self, text: str, ctx: WorkflowContext[str, str]) -> None:
        response = await self.agent.run(text)
        if self.final:
            await ctx.yield_output(response.text)
        else:
            await ctx.send_message(response.text)


def build_workflow(request: HostedResponseRequest) -> Workflow:
    intake = IntakeExecutor()
    triage = AgentStepExecutor(
        DeskAgent("triage", "Classify the ticket as network, facilities or procurement and give a priority in one line.")
    )
    reply = AgentStepExecutor(
        DeskAgent("reply", "Write a two sentence customer reply that names the team and the priority."), final=True
    )
    return (
        WorkflowBuilder(name="m6-ticket-workflow", start_executor=intake, output_from=[reply])
        .add_edge(intake, triage)
        .add_edge(triage, reply)
        .build()
    )


async def parse_response(request: HostedResponseRequest) -> WorkflowTurn[Ticket]:
    return WorkflowTurn(input=Ticket.model_validate_json(await request.get_input_text() or ""))


if __name__ == "__main__":
    ResponsesHostServer(
        workflow=build_workflow,
        parse_response=parse_response,
        checkpoint_store_provider=CheckpointStoreProvider(
            allowed_checkpoint_types=[f"{Ticket.__module__}:{Ticket.__qualname__}"],
        ),
    ).run()
