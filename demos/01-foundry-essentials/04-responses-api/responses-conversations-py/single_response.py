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

response = openai.responses.create(
    model=model,
    instructions="You are a concise travel assistant.",
    input="Name one must-see place in Salzburg.",
)

print(f"id      : {response.id}")
print(f"status  : {response.status}")
print(f"output  : {[item.type for item in response.output]}")
print(f"text    : {response.output_text}")

stored = openai.responses.retrieve(response.id)
print(f"stored  : {stored.id == response.id}")

openai.responses.delete(response.id)
print("deleted : True")
