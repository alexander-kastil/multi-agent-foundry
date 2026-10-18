import json
import os
import time
from common import connect

with open("response_ids.json") as file:
    response_ids = json.load(file)

with connect() as (project_client, openai_client):
    evaluation = openai_client.evals.create(
        name="m7-response-evaluation",
        data_source_config={"type": "azure_ai_source", "scenario": "responses"},
        testing_criteria=[
            {"type": "azure_ai_evaluator", "name": "violence", "evaluator_name": "builtin.violence"},
            {
                "type": "azure_ai_evaluator",
                "name": "coherence",
                "evaluator_name": "builtin.coherence",
                "initialization_parameters": {"deployment_name": os.environ["MODEL_DEPLOYMENT_NAME"]},
            },
        ],
    )
    run = openai_client.evals.runs.create(
        eval_id=evaluation.id,
        name="m7-stored-responses",
        data_source={
            "type": "azure_ai_responses",
            "item_generation_params": {
                "type": "response_retrieval",
                "data_mapping": {"response_id": "{{item.resp_id}}"},
                "source": {"type": "file_content", "content": [{"item": {"resp_id": response_id}} for response_id in response_ids]},
            },
        },
    )
    while run.status not in ("completed", "failed", "canceled"):
        time.sleep(10)
        run = openai_client.evals.runs.retrieve(run_id=run.id, eval_id=evaluation.id)
    print(f"run {run.status}  {run.result_counts}")
    for output_item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id):
        verdicts = " ".join(f"{result.name}={result.score}" for result in output_item.results)
        print(f"{output_item.datasource_item['response_id'][:18]}  {verdicts}")
