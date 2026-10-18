import os
import sys

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import MCPToolboxTool
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
name = os.environ["TOOLBOX_NAME"]

if len(sys.argv) > 1 and sys.argv[1] == "publish":
    toolbox = project.toolboxes.update(name, default_version=sys.argv[2])
    print(f"default version is now {toolbox.default_version}")
else:
    version = project.toolboxes.create_version(
        name=name,
        description="Snippets only",
        tools=[
            MCPToolboxTool(
                server_label="snippets",
                server_url=os.environ["SNIPPETS_MCP_URL"],
                project_connection_id=os.environ["SNIPPETS_CONNECTION_NAME"],
                description="Save and read named code snippets",
                require_approval="never",
            )
        ],
    )
    toolbox = project.toolboxes.get(name)
    print(f"created version {version.version}, default version is still {toolbox.default_version}")
