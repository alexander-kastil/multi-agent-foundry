import os
from contextlib import contextmanager
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.identity import DefaultAzureCredential

load_dotenv(override=True)

PRICING_AGENT_NAME = "m7-pricing-agent"

PRICING_PROMPTS = {
    "v1": (
        "You are a pricing agent. Given an order total and a discount percent, compute the final price. "
        "Reply with the numeric value only, with two decimals, no currency symbol and no other text."
    ),
    "v2": (
        "You are a pricing agent. Given an order total and a discount percent, compute the final price. "
        "Always include the currency symbol and add a one sentence explanation of the discount for clarity."
    ),
}


@contextmanager
def connect():
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):
        yield project_client, openai_client


def deploy_pricing_versions(project_client):
    versions = {}
    for label, prompt in PRICING_PROMPTS.items():
        agent = project_client.agents.create_version(
            agent_name=PRICING_AGENT_NAME,
            definition=PromptAgentDefinition(model=os.environ["MODEL_DEPLOYMENT_NAME"], instructions=prompt),
        )
        versions[label] = agent.version
    return versions


def delete_pricing_versions(project_client, versions):
    for version in versions.values():
        project_client.agents.delete_version(agent_name=PRICING_AGENT_NAME, agent_version=version)


def ask_pricing_agent(openai_client, version, text):
    return openai_client.responses.create(
        input=text,
        extra_body={"agent_reference": {"name": PRICING_AGENT_NAME, "version": version, "type": "agent_reference"}},
    ).output_text
