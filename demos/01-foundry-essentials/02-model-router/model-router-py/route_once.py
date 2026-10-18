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
    model=os.environ["MODEL_ROUTER_DEPLOYMENT_NAME"],
    input="In one sentence, name the most popular tourist destination in Seattle.",
)

print(f"deployment  : {os.environ['MODEL_ROUTER_DEPLOYMENT_NAME']}")
print(f"served by   : {response.model}")
print(f"answer      : {response.output_text}")
