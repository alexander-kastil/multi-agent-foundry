import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition

load_dotenv(override=True)

AGENT_NAME = os.getenv("AGENT_NAME", "m3-support-agent")
MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]

V1_INSTRUCTIONS = "You are a support agent for Contoso Outdoor. Answer in one short sentence."
V2_INSTRUCTIONS = (
    "You are a support agent for Contoso Outdoor. "
    "Answer in two short sentences and end with a follow-up question."
)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()


def reference(version):
    return {"agent_reference": {"name": AGENT_NAME, "version": version, "type": "agent_reference"}}


def create_version(instructions):
    agent = project.agents.create_version(
        agent_name=AGENT_NAME,
        definition=PromptAgentDefinition(model=MODEL, instructions=instructions),
    )
    print(f"Created {agent.name}:{agent.version}")
    return agent


print("== Step 1: create version 1")
v1 = create_version(V1_INSTRUCTIONS)

print("== Step 2: single response, no conversation")
response = openai.responses.create(
    input="My tent zipper is stuck. What should I do?",
    extra_body=reference(v1.version),
)
print(f"{AGENT_NAME}:{v1.version} -> {response.output_text}")

print("== Step 3: conversation with history")
conversation = openai.conversations.create(
    items=[{"type": "message", "role": "user", "content": "I bought the Trailhead 2 tent last week."}]
)
print(f"Conversation: {conversation.id}")
for question in ["Which tent did I buy?", "How long ago was that?"]:
    turn = openai.responses.create(
        conversation=conversation.id,
        input=question,
        extra_body=reference(v1.version),
    )
    print(f"user: {question}\nagent: {turn.output_text}")

print("== Step 4: items in the conversation")
for item in openai.conversations.items.list(conversation.id, order="asc"):
    text = item.content[0].text if item.type == "message" else ""
    print(f"{item.type:<8} {getattr(item, 'role', ''):<10} {text}")

print("== Step 5: create version 2 and compare")
v2 = create_version(V2_INSTRUCTIONS)
for version in (v1.version, v2.version):
    answer = openai.responses.create(
        input="My tent zipper is stuck. What should I do?",
        extra_body=reference(version),
    )
    print(f"{AGENT_NAME}:{version} -> {answer.output_text}")

print("== Step 6: same conversation continues on version 2")
turn = openai.responses.create(
    conversation=conversation.id,
    input="Summarize what we discussed.",
    extra_body=reference(v2.version),
)
print(f"{AGENT_NAME}:{v2.version} -> {turn.output_text}")

print("== Versions")
for version in project.agents.list_versions(AGENT_NAME):
    print(f"{version.name}:{version.version} model={version.definition.model}")

print("== Cleanup")
openai.conversations.delete(conversation.id)
for version in (v1.version, v2.version):
    project.agents.delete_version(agent_name=AGENT_NAME, agent_version=version)
    print(f"Deleted {AGENT_NAME}:{version}")
