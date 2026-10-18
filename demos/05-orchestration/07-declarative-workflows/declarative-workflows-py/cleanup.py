import os

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

with AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential(), allow_preview=True) as project:
    for name in ("m5-support-router", "m5-classifier", "m5-billing", "m5-technical"):
        project.agents.delete(name)
        print(f"deleted {name}")
