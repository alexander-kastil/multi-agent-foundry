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

first = openai.responses.create(model=model, input="My favourite city is Graz. Remember that.")
second = openai.responses.create(
    model=model,
    input="Which city did I say was my favourite?",
    previous_response_id=first.id,
)
print(f"chained   : {second.output_text}")

stateless_first = openai.responses.create(
    model=model,
    input="My favourite city is Graz. Remember that.",
    store=False,
)
stateless_second = openai.responses.create(
    model=model,
    input=[
        {"role": "user", "content": "My favourite city is Graz. Remember that."},
        {"role": "assistant", "content": stateless_first.output_text},
        {"role": "user", "content": "Which city did I say was my favourite?"},
    ],
    store=False,
)
print(f"stateless : {stateless_second.output_text}")

for response_id in (first.id, second.id):
    openai.responses.delete(response_id)
