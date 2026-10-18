import os
import time
from datetime import datetime, timezone
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import TestingCriterionAzureAIEvaluator
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from openai.types.eval_create_params import DataSourceConfigCustom
from openai.types.evals.create_eval_jsonl_run_data_source_param import CreateEvalJSONLRunDataSourceParam, SourceFileID

load_dotenv(override=True)

EVALUATORS = {
    "task_completion": ["query", "response", "tool_definitions"],
    "task_adherence": ["query", "response"],
    "intent_resolution": ["query", "response"],
    "tool_call_accuracy": ["query", "response", "tool_definitions"],
    "tool_selection": ["query", "response", "tool_definitions"],
    "tool_input_accuracy": ["query", "response", "tool_definitions"],
    "tool_output_utilization": ["query", "response", "tool_definitions"],
    "tool_call_success": ["response"],
}

LABELS = {
    "task_completion": "completion",
    "task_adherence": "adherence",
    "intent_resolution": "intent",
    "tool_call_accuracy": "call_acc",
    "tool_selection": "selection",
    "tool_input_accuracy": "input_acc",
    "tool_output_utilization": "output_use",
    "tool_call_success": "call_ok",
}

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    dataset = project_client.datasets.upload_file(
        name="m7-agent-trajectories",
        version=datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S"),
        file_path="agent_trajectories.jsonl",
    )
    data_source_config = DataSourceConfigCustom(
        type="custom",
        item_schema={
            "type": "object",
            "properties": {
                "id": {"type": "string"},
                "scenario": {"type": "string"},
                "query": {"type": "array"},
                "response": {"type": "array"},
                "tool_definitions": {"type": "array"},
            },
            "required": ["query", "response", "tool_definitions"],
        },
    )
    testing_criteria = [
        TestingCriterionAzureAIEvaluator(
            type="azure_ai_evaluator",
            name=name,
            evaluator_name=f"builtin.{name}",
            initialization_parameters={"deployment_name": os.environ["JUDGE_MODEL_DEPLOYMENT_NAME"]},
            data_mapping={field: "{{item." + field + "}}" for field in fields},
        )
        for name, fields in EVALUATORS.items()
    ]
    evaluation = openai_client.evals.create(
        name="m7-agent-evaluators", data_source_config=data_source_config, testing_criteria=testing_criteria
    )
    run = openai_client.evals.runs.create(
        eval_id=evaluation.id,
        name="m7-agent-trajectories-run",
        data_source=CreateEvalJSONLRunDataSourceParam(type="jsonl", source=SourceFileID(type="file_id", id=dataset.id)),
    )
    while run.status not in ("completed", "failed", "canceled"):
        time.sleep(10)
        run = openai_client.evals.runs.retrieve(run_id=run.id, eval_id=evaluation.id)
    print(f"status {run.status}  {run.result_counts}")
    print(f"report {run.report_url}")

    verdicts = {}
    for output_item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id):
        scenario = output_item.datasource_item["scenario"]
        verdicts[output_item.datasource_item["id"] + " " + scenario] = {
            result.name: {True: "pass", False: "fail", None: "n/a"}[result.passed] for result in output_item.results
        }

    print(f"{'trajectory':24}" + "".join(f"{LABELS[name]:12}" for name in EVALUATORS))
    for key in sorted(verdicts):
        print(f"{key:24}" + "".join(f"{verdicts[key].get(name, '-'):12}" for name in EVALUATORS))
