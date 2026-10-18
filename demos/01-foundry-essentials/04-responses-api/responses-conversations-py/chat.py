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

conversation = openai.conversations.create(metadata={"demo": "m1-chat"})
print(f"conversation: {conversation.id}")

for question in sys.argv[1:]:
    print(f"\nyou: {question}")
    print("bot: ", end="", flush=True)
    stream = openai.responses.create(
        model=model,
        conversation=conversation.id,
        instructions="You are a friendly assistant. Answer in at most two sentences.",
        input=question,
        stream=True,
    )
    for event in stream:
        if event.type == "response.output_text.delta":
            print(event.delta, end="", flush=True)
    print()

items = openai.conversations.items.list(conversation.id)
print(f"\nitems stored: {len(list(items))}")
openai.conversations.delete(conversation.id)
print("conversation deleted")
