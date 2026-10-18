import os
import sys

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

from loop_core import AGENT_NAME, INSTRUCTIONS, MARKET_DATA_TOOL, fork_scenarios, plan_then_act, reflect, run_turn
from session_store import save_session

sys.stdout.reconfigure(encoding="utf-8")
load_dotenv(override=True)

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    agent = project_client.agents.create_version(
        agent_name=AGENT_NAME,
        definition=PromptAgentDefinition(
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            instructions=INSTRUCTIONS,
            tools=[MARKET_DATA_TOOL],
        ),
    )
    print(f"Agent {agent.name} version {agent.version}")

    print("\nPlan then act")
    acted = plan_then_act(openai_client, "Evaluate MSFT as a long-term investment")

    print("\nAct then reflect")
    reflected, trace = reflect(openai_client, acted)
    print(f"Recommendation: {trace['recommendation']}")

    print("\nFork into scenarios")
    for name, job in fork_scenarios(openai_client, reflected).items():
        print(f"[{name}] {job.output_text}")

    print("\nPersist the session")
    conversation = openai_client.conversations.create()
    first = run_turn(openai_client, "Fetch market data for GOOG and summarize it in one sentence.", conversation_id=conversation.id)
    save_session("analyst-session-1", conversation.id, first.id, {"ticker": "GOOG", "workflow": "research", "agent_version": agent.version})
    print(f"Saved analyst-session-1 -> {conversation.id}")
