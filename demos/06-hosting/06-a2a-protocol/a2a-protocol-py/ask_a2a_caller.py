import os
import sys

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai_client = project.get_openai_client()

response = openai_client.responses.create(
    tool_choice="required",
    input="Ask the release desk what it covers.",
    extra_body={"agent_reference": {"name": "m6h2-a2a-caller", "type": "agent_reference"}},
)
print(response.output_text)
print([item.type for item in response.output])
