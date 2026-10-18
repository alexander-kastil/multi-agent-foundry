import os
from contextlib import contextmanager
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.identity import DefaultAzureCredential
from policy import AGENT_INSTRUCTIONS, AGENT_NAME

load_dotenv(override=True)


@contextmanager
def connect():
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):
        yield project_client, openai_client


def deploy_versions(project_client):
    versions = {}
    for label, instructions in AGENT_INSTRUCTIONS.items():
        agent = project_client.agents.create_version(
            agent_name=AGENT_NAME,
            definition=PromptAgentDefinition(model=os.environ["MODEL_DEPLOYMENT_NAME"], instructions=instructions),
        )
        versions[label] = agent.version
    return versions


def delete_versions(project_client, versions):
    for version in versions.values():
        project_client.agents.delete_version(agent_name=AGENT_NAME, agent_version=version)


def ask_agent(openai_client, version, question):
    response = openai_client.responses.create(
        input=question,
        extra_body={"agent_reference": {"name": AGENT_NAME, "version": version, "type": "agent_reference"}},
    )
    return response.output_text
