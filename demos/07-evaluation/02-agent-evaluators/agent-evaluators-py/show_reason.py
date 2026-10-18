import os
import sys
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

trajectory_id, evaluator = sys.argv[1], sys.argv[2]

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    evaluation = next(item for item in openai_client.evals.list() if item.name == "m7-agent-evaluators")
    run = next(iter(openai_client.evals.runs.list(eval_id=evaluation.id)))
    for output_item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id):
        if output_item.datasource_item["id"] == trajectory_id:
            for result in output_item.results:
                if result.name == evaluator:
                    print(f"{trajectory_id} {evaluator} passed={result.passed} score={result.score}")
                    print(result.reason)
