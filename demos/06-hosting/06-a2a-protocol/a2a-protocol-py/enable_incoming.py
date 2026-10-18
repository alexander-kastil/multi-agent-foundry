import os

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    A2AProtocolConfiguration,
    AgentCard,
    AgentCardSkill,
    AgentEndpointConfig,
    PromptAgentDefinition,
    ProtocolConfiguration,
    ResponsesProtocolConfiguration,
)
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())

agent = project.agents.create_version(
    agent_name="m6h2-a2a-agent",
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions="You are the Contoso release desk. Answer questions about product releases in two sentences.",
    ),
)
print(f"agent {agent.name} version {agent.version}")

patched = project.agents.update_details(
    agent_name="m6h2-a2a-agent",
    agent_endpoint=AgentEndpointConfig(
        protocol_configuration=ProtocolConfiguration(
            responses=ResponsesProtocolConfiguration(),
            a2a=A2AProtocolConfiguration(),
        ),
    ),
    agent_card=AgentCard(
        version="1.0",
        description="Answers questions about Contoso product releases.",
        skills=[AgentCardSkill(id="releases", name="Release questions", description="Explains what shipped and when.")],
    ),
)
print(f"incoming A2A enabled for {patched.name}")
