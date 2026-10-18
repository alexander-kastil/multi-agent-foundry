import os

import httpx
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

credential = DefaultAzureCredential()
project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential)

for name in ("m6h2-a2a-caller", "m6h2-a2a-agent"):
    project.agents.delete(name)
    print(f"deleted agent {name}")

connection_url = (
    f"https://management.azure.com/subscriptions/{os.environ['SUBSCRIPTION_ID']}"
    f"/resourceGroups/{os.environ['RESOURCE_GROUP']}/providers/Microsoft.CognitiveServices"
    f"/accounts/{os.environ['ACCOUNT_NAME']}/projects/{os.environ['PROJECT_NAME']}"
    f"/connections/m6h2-a2a-target?api-version=2025-04-01-preview"
)
arm_token = credential.get_token("https://management.azure.com/.default").token
httpx.delete(connection_url, headers={"Authorization": f"Bearer {arm_token}"}).raise_for_status()
print("deleted connection m6h2-a2a-target")
