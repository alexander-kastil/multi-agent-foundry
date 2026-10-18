import requests
from azure.identity import get_bearer_token_provider
from azure.ai.projects.models import MCPTool, PromptAgentDefinition

from common import AGENT, API_VERSION, CONNECTION, KNOWLEDGE_BASE, MODEL, PROJECT_RESOURCE_ID, SEARCH_ENDPOINT, credential, openai, project

MCP_ENDPOINT = f"{SEARCH_ENDPOINT}/knowledgebases/{KNOWLEDGE_BASE}/mcp?api-version={API_VERSION}"
INSTRUCTIONS = """You are a helpful assistant for Contoso Outdoor.
Use the knowledge base tool to answer every question.
If the knowledge base does not contain the answer, respond with "I don't know".
When you use information from the knowledge base, include citations to the retrieved sources."""

print("== Step 4: create the project connection")
management_token = get_bearer_token_provider(credential, "https://management.azure.com/.default")()
response = requests.put(
    f"https://management.azure.com{PROJECT_RESOURCE_ID}/connections/{CONNECTION}?api-version=2025-10-01-preview",
    headers={"Authorization": f"Bearer {management_token}"},
    json={
        "name": CONNECTION,
        "type": "Microsoft.MachineLearningServices/workspaces/connections",
        "properties": {
            "authType": "ProjectManagedIdentity",
            "category": "RemoteTool",
            "target": MCP_ENDPOINT,
            "isSharedToAll": True,
            "audience": "https://search.azure.com/",
            "metadata": {"ApiType": "Azure"},
        },
    },
)
response.raise_for_status()
print(f"connection {CONNECTION} -> {MCP_ENDPOINT}")

print("== Step 5: attach the knowledge base to an agent as an MCP tool")
agent = project.agents.create_version(
    agent_name=AGENT,
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions=INSTRUCTIONS,
        tools=[
            MCPTool(
                server_label="knowledge-base",
                server_url=MCP_ENDPOINT,
                require_approval="never",
                allowed_tools=["knowledge_base_retrieve"],
                project_connection_id=CONNECTION,
            )
        ],
    ),
)
print(f"Created {agent.name}:{agent.version}")

questions = [
    "Which of your tents is rated for winter storms, and what does it cost?",
    "If that tent arrives damaged, what must I do and how long do I have? Does the warranty cover seam failures?",
    "Do you sell snowshoes?",
]
conversation = openai.conversations.create()
for question in questions:
    reply = openai.responses.create(
        input=question,
        conversation=conversation.id,
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    )
    print(f"\nuser: {question}")
    print("items:", ", ".join(item.type for item in reply.output))
    print(f"agent: {reply.output_text}")

print("\n== Cleanup")
openai.conversations.delete(conversation.id)
project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
print(f"Deleted {agent.name}:{agent.version}")
