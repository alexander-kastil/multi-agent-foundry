import json
import os
import sys
import time
from contextlib import contextmanager
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
from azure.identity import DefaultAzureCredential

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

AGENT_NAME = "m7-ticket-triage-agent"
DATASET_NAME = "m7-ticket-triage"
TOOL_AGENT_NAME = "m7-tool-agent"
EVALUATOR_NAME = "m7_tool_choice"
EVALUATION_FEATURES = {"Foundry-Features": "Evaluations=V1Preview"}
BASELINE_INSTRUCTIONS = "You triage customer support tickets for a chocolate shop."


@contextmanager
def connect():
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):
        yield project_client, openai_client


def load_tickets():
    with open("tickets.jsonl") as file:
        return [json.loads(line) for line in file]


def create_agent_version(project_client, instructions):
    return project_client.agents.create_version(
        agent_name=AGENT_NAME,
        definition=PromptAgentDefinition(model=os.environ["MODEL_DEPLOYMENT_NAME"], instructions=instructions),
    )


def score_version(project_client, openai_client, version):
    dataset = project_client.datasets.upload_file(
        name=DATASET_NAME, version=f"{version}-{int(time.time())}", file_path="tickets.jsonl"
    )
    evaluation = openai_client.evals.create(
        name=f"m7-triage-v{version}",
        data_source_config={
            "type": "custom",
            "item_schema": {
                "type": "object",
                "properties": {"query": {"type": "string"}, "ground_truth": {"type": "string"}},
                "required": ["query", "ground_truth"],
            },
            "include_sample_schema": True,
        },
        testing_criteria=[
            {
                "type": "azure_ai_evaluator",
                "name": "f1",
                "evaluator_name": "builtin.f1_score",
                "data_mapping": {"response": "{{sample.output_text}}", "ground_truth": "{{item.ground_truth}}"},
            }
        ],
    )
    run = openai_client.evals.runs.create(
        eval_id=evaluation.id,
        name=f"m7-triage-v{version}-run",
        data_source={
            "type": "azure_ai_target_completions",
            "source": {"type": "file_id", "id": dataset.id},
            "input_messages": {
                "type": "template",
                "template": [{"type": "message", "role": "user", "content": {"type": "input_text", "text": "{{item.query}}"}}],
            },
            "target": {"type": "azure_ai_agent", "name": AGENT_NAME, "version": str(version)},
        },
    )
    while run.status not in ("completed", "failed", "canceled"):
        time.sleep(10)
        run = openai_client.evals.runs.retrieve(run_id=run.id, eval_id=evaluation.id)
    scores = [
        result.score
        for output_item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id)
        for result in output_item.results
    ]
    return sum(scores) / len(scores), len(scores)
