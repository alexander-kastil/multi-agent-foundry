import os

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import MCPTool, PromptAgentDefinition
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()

AGENT_NAME = "m2-functions-mcp-agent"

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()

functions_tool = MCPTool(
    server_label="snippets",
    server_url=os.environ["MCP_SERVER_URL"],
    project_connection_id=os.environ["MCP_CONNECTION_NAME"],
    require_approval="never",
)

agent = project.agents.create_version(
    agent_name=AGENT_NAME,
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions="You manage code snippets. Use the snippets tools to save and read them.",
        tools=[functions_tool],
    ),
)
print(f"agent {agent.name} version {agent.version}")

conversation = openai.conversations.create()
agent_ref = {"agent_reference": {"name": agent.name, "type": "agent_reference"}}

for prompt in [
    "Save a snippet named fizz with the content: for i in range(3): print(i)",
    "Read the snippet named fizz back to me.",
]:
    response = openai.responses.create(conversation=conversation.id, input=prompt, extra_body=agent_ref)
    calls = [item for item in response.output if item.type == "mcp_call"]
    for call in calls:
        print(f"mcp_call {call.name} {call.arguments}")
    print(response.output_text)

openai.conversations.delete(conversation.id)
project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
print("cleaned up")
