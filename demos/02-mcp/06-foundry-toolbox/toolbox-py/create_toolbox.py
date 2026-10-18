import os

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import MCPToolboxTool
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())

version = project.toolboxes.create_version(
    name=os.environ["TOOLBOX_NAME"],
    description="Microsoft Learn search plus the snippets server hosted on Azure Functions",
    tools=[
        MCPToolboxTool(
            server_label="mslearn",
            server_url="https://learn.microsoft.com/api/mcp",
            description="Search and read Microsoft Learn documentation",
            require_approval="never",
        ),
        MCPToolboxTool(
            server_label="snippets",
            server_url=os.environ["SNIPPETS_MCP_URL"],
            project_connection_id=os.environ["SNIPPETS_CONNECTION_NAME"],
            description="Save and read named code snippets",
            require_approval="never",
        ),
    ],
)

toolbox = project.toolboxes.get(os.environ["TOOLBOX_NAME"])
print(f"toolbox {toolbox.name}: created version {version.version}, default version {toolbox.default_version}")
print(f"consumer endpoint: {os.environ['PROJECT_ENDPOINT']}/toolboxes/{toolbox.name}/mcp?api-version=v1")
print(f"version endpoint:  {os.environ['PROJECT_ENDPOINT']}/toolboxes/{toolbox.name}/versions/{version.version}/mcp?api-version=v1")
