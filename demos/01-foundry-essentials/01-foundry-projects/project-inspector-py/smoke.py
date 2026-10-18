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
    input="Reply with the single word: ready",
)
print(f"base_url : {openai.base_url}")
print(f"model    : {response.model}")
print(f"output   : {response.output_text}")
