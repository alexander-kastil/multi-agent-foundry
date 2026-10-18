import os
import sys

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

MESSAGES = sys.argv[1:] or ["I was charged twice this month."]

with AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential(), allow_preview=True) as project:
    openai = project.get_openai_client()
    for message in MESSAGES:
        conversation = openai.conversations.create()
        response = openai.responses.create(
            conversation=conversation.id,
            input=message,
            extra_body={"agent_reference": {"name": "m5-support-router", "type": "agent_reference"}},
        )
        print(f"{message} -> {response.output_text}")
        openai.conversations.delete(conversation.id)
