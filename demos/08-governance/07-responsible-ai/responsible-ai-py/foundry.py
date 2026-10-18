import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

load_dotenv()

MODEL = os.getenv("MODEL_DEPLOYMENT_NAME")

project = AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=DefaultAzureCredential())
openai_client = project.get_openai_client()


def create_agent(name, instructions, metadata=None):
    return project.agents.create_version(
        agent_name=name,
        definition=PromptAgentDefinition(model=MODEL, instructions=instructions),
        metadata=metadata,
    )


def ask(agent, text):
    return openai_client.responses.create(
        input=text,
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    ).output_text


def delete_agent(agent):
    project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
