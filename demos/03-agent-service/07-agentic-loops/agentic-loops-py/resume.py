import os
import sys

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

from loop_core import AGENT_NAME, run_turn
from session_store import load_session

sys.stdout.reconfigure(encoding="utf-8")
load_dotenv(override=True)

saved = load_session("analyst-session-1")
print(f"Loaded session: conversation {saved['conversation_id']}, metadata {saved['metadata']}")

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    resumed = run_turn(
        openai_client,
        "Which ticker did we just discuss, and what was its P/E ratio?",
        conversation_id=saved["conversation_id"],
    )
    print(f"Resumed: {resumed.output_text}")

    openai_client.conversations.delete(saved["conversation_id"])
    project_client.agents.delete_version(agent_name=AGENT_NAME, agent_version=saved["metadata"]["agent_version"])
    print("Deleted conversation and agent version")
