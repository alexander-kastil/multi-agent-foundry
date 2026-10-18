import os

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

with AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential()
) as project:
    project.agents.delete(os.environ["FOUNDRY_AGENT_NAME"])
    print(f"deleted {os.environ['FOUNDRY_AGENT_NAME']}")
