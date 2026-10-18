import os

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())

for name in ("m6-weekday-digest", "m6-one-shot", "m6-azd-digest"):
    project.beta.routines.delete(name)
    print(f"deleted routine {name}")

project.agents.delete(os.environ["AGENT_NAME"])
print(f"deleted agent {os.environ['AGENT_NAME']}")
