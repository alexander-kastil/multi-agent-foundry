import os

import httpx
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import A2AProtocolVersion, A2ATool, PromptAgentDefinition
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

CONNECTION_NAME = "m6h2-a2a-target"
credential = DefaultAzureCredential()
project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential)

connection_url = (
    f"https://management.azure.com/subscriptions/{os.environ['SUBSCRIPTION_ID']}"
    f"/resourceGroups/{os.environ['RESOURCE_GROUP']}/providers/Microsoft.CognitiveServices"
    f"/accounts/{os.environ['ACCOUNT_NAME']}/projects/{os.environ['PROJECT_NAME']}"
    f"/connections/{CONNECTION_NAME}?api-version=2025-04-01-preview"
)
arm_token = credential.get_token("https://management.azure.com/.default").token
httpx.put(
    connection_url,
    headers={"Authorization": f"Bearer {arm_token}"},
    json={
        "name": CONNECTION_NAME,
        "type": "Microsoft.MachineLearningServices/workspaces/connections",
        "properties": {
            "authType": "AgenticIdentityToken",
            "group": "ServicesAndApps",
            "category": "RemoteA2A",
            "target": f"{os.environ['PROJECT_ENDPOINT']}/agents/m6h2-a2a-agent/endpoint/protocols/a2a",
            "isSharedToAll": True,
            "sharedUserList": [],
            "audience": "https://ai.azure.com",
            "Credentials": {},
            "metadata": {"ApiType": "Azure"},
        },
    },
).raise_for_status()
print(f"connection {CONNECTION_NAME} created")

connection = project.connections.get(CONNECTION_NAME)

caller = project.agents.create_version(
    agent_name="m6h2-a2a-caller",
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions="You are a helpful assistant. Use the A2A tool to delegate release questions to the release desk.",
        tools=[A2ATool(a2a_version=A2AProtocolVersion.V1_0, project_connection_id=connection.id)],
    ),
)
print(f"agent {caller.name} version {caller.version}")

details = project.agents.get(caller.name)
print(f"caller identity: {details['instance_identity']['principal_id']}")
