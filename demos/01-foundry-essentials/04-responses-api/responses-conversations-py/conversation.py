import os
import sys
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)
openai = project.get_openai_client()
model = os.environ["MODEL_DEPLOYMENT_NAME"]

conversation = openai.conversations.create(
    items=[{"type": "message", "role": "user", "content": "I am planning a trip from Vienna."}],
    metadata={"demo": "m1-conversation"},
)
print(f"conversation : {conversation.id}")

for question in ("Which country is my departure city in?", "And which river runs through it?"):
    response = openai.responses.create(
        model=model,
        conversation=conversation.id,
        instructions="Answer in one short sentence.",
        input=question,
    )
    print(f"user         : {question}")
    print(f"assistant    : {response.output_text}")

openai.conversations.items.create(
    conversation_id=conversation.id,
    items=[{"type": "message", "role": "user", "content": "Add Linz as a second stop."}],
)

print()
for item in openai.conversations.items.list(conversation.id, order="asc"):
    role = getattr(item, "role", "-")
    print(f"{item.type:<10} {role:<10} {item.id}")

openai.conversations.delete(conversation.id)
print()
print("conversation deleted")
