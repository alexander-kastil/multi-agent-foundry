import os
from urllib.parse import urlsplit
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import MCPTool, PromptAgentDefinition

load_dotenv()
NAME = "m8-idn-github"

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=credential) as project,
    project.get_openai_client() as openai_client,
):
    agent = project.agents.create_version(
        agent_name=NAME,
        definition=PromptAgentDefinition(
            model=os.getenv("MODEL_DEPLOYMENT_NAME"),
            instructions="Use the GitHub tool to answer.",
            tools=[
                MCPTool(
                    server_label="github",
                    server_url="https://api.githubcopilot.com/mcp/",
                    require_approval="never",
                    project_connection_id=os.getenv("OAUTH_CONNECTION_NAME"),
                )
            ],
        ),
    )
    response = openai_client.responses.create(
        input="Which GitHub user am I?",
        extra_body={"agent_reference": {"name": NAME, "version": agent.version, "type": "agent_reference"}},
    )
    for item in response.output:
        print(f"output item: {item.type}")
        if item.type == "oauth_consent_request":
            print(f"  consent host: {urlsplit(item.consent_link).netloc}")
            print(f"  response id:  {response.id}")
    project.agents.delete_version(agent_name=NAME, agent_version=agent.version)
