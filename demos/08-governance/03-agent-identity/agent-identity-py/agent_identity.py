import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

load_dotenv()

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=credential) as project,
):
    for name in ("m8-idn-orders", "m8-idn-hr"):
        project.agents.create_version(
            agent_name=name,
            definition=PromptAgentDefinition(model=os.getenv("MODEL_DEPLOYMENT_NAME"), instructions="You answer in one sentence."),
        )
        agent = project.agents.get(agent_name=name)
        print(name)
        print(f"  agent identity principal_id: {agent.instance_identity['principal_id']}")
        print(f"  agent identity client_id:    {agent.instance_identity['client_id']}")
        print(f"  blueprint reference:         {agent.blueprint_reference['blueprint_id']}")
        print(f"  inbound authorization:       {agent.agent_endpoint.authorization_schemes[0].type}")
