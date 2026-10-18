import os

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

with AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential()
) as project:
    version = project.agents.create_version(
        agent_name=os.environ["FOUNDRY_AGENT_NAME"],
        definition=PromptAgentDefinition(
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            instructions="You are the returns desk of Contoso Outdoor. Answer in two sentences and always quote the 30 day return window.",
        ),
    )
    print(f"created {version.name} version {version.version}")
