import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    AgentEndpointConfig,
    FixedRatioVersionSelectionRule,
    PromptAgentDefinition,
    VersionSelector,
)

load_dotenv()

MODEL = os.getenv("MODEL_DEPLOYMENT_NAME")

project = AIProjectClient(
    endpoint=os.getenv("PROJECT_ENDPOINT"),
    credential=DefaultAzureCredential(),
    allow_preview=True,
)
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


def ask_endpoint(agent_name, text):
    with project.get_openai_client(agent_name=agent_name) as client:
        return client.responses.create(input=text).output_text


def pin_endpoint(agent_name, version):
    return project.agents.update_details(
        agent_name=agent_name,
        agent_endpoint=AgentEndpointConfig(
            version_selector=VersionSelector(
                version_selection_rules=[FixedRatioVersionSelectionRule(agent_version=version, traffic_percentage=100)]
            )
        ),
    )


def served_version(agent_name):
    rule = project.agents.get(agent_name=agent_name).agent_endpoint.version_selector.version_selection_rules[0]
    return rule.agent_version


def delete_agent(agent):
    project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
