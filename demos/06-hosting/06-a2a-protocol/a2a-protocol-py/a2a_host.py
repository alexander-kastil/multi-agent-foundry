import os

import uvicorn
from a2a.server.request_handlers import DefaultRequestHandler
from a2a.server.routes import create_agent_card_routes, create_jsonrpc_routes
from a2a.server.tasks import InMemoryTaskStore
from a2a.types import AgentCapabilities, AgentCard, AgentInterface, AgentSkill
from agent_framework import Agent
from agent_framework.a2a import A2AExecutor
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from starlette.applications import Starlette

load_dotenv(override=True)


def create_agent(name: str, instructions: str) -> Agent:
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    return Agent(client=client, name=name, instructions=instructions)


def serve(agent: Agent, description: str, skill: AgentSkill, port: int) -> None:
    card = AgentCard(
        name=agent.name,
        description=description,
        version="1.0.0",
        default_input_modes=["text"],
        default_output_modes=["text"],
        capabilities=AgentCapabilities(streaming=False),
        supported_interfaces=[AgentInterface(url=f"http://localhost:{port}/", protocol_binding="JSONRPC")],
        skills=[skill],
    )
    handler = DefaultRequestHandler(
        agent_executor=A2AExecutor(agent),
        task_store=InMemoryTaskStore(),
        agent_card=card,
    )
    app = Starlette(routes=[*create_agent_card_routes(card), *create_jsonrpc_routes(handler, "/")])
    uvicorn.run(app, host="127.0.0.1", port=port)
