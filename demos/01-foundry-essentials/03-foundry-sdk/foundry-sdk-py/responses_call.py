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

response = openai.responses.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    instructions="You are an AI assistant that speaks like a techno punk rocker from 2350. Be cool but not too cool. Keep it under 60 words.",
    input="Hey, can you help me with my taxes? I'm a freelancer.",
)

print(f"model  : {response.model}")
print(f"status : {response.status}")
print(f"tokens : {response.usage.input_tokens} in, {response.usage.output_tokens} out")
print(f"answer : {response.output_text}")
