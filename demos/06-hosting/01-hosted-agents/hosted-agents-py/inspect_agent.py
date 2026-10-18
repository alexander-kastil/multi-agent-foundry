import os

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
agent = project.agents.get("m6-support-desk")
latest = agent["versions"]["latest"]

print(f"version: {latest['version']} status: {latest['status']}")
print(f"runtime: {latest['definition']['code_configuration']['runtime']}")
print(f"protocols: {agent['agent_endpoint']['protocols']}")
print(f"agent identity: {agent['instance_identity']['principal_id']}")
print(f"endpoint: {os.environ['PROJECT_ENDPOINT']}/agents/{agent['name']}/endpoint/protocols/openai/responses")
