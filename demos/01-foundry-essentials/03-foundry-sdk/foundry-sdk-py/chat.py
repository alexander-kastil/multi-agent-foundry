import sys
import os

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

sys.stdout.reconfigure(encoding="utf-8")
load_dotenv(override=True)

model = os.environ["MODEL_DEPLOYMENT_NAME"]
project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()

response = openai.responses.create(
    model=model,
    instructions="You are an AI assistant that speaks like a techno punk rocker from 2350. Be cool but not too cool. Keep it under 60 words.",
    input="Hey, can you help me with my taxes? I'm a freelancer.",
)
print("--- Responses API ---")
print(response.output_text)

completion = openai.chat.completions.create(
    model=model,
    messages=[
        {"role": "system", "content": "You are an AI assistant that speaks like a techno punk rocker from 2350. Be cool but not too cool. Keep it under 60 words."},
        {"role": "user", "content": "Hey, can you help me with my taxes? I'm a freelancer."},
    ],
)
print("\n--- Chat Completions API ---")
print(completion.choices[0].message.content)
