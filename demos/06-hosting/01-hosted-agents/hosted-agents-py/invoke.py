import os
import sys

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai_client = project.get_openai_client(agent_name="m6-support-desk")

response = openai_client.responses.create(input=sys.argv[1] if len(sys.argv) > 1 else "What is the status of ticket T-1002?")
print(response.output_text)
