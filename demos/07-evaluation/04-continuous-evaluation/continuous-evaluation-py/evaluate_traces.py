import os
import time
from common import AGENT_NAME, connect

with connect() as (project_client, openai_client):
    agent = project_client.agents.get(AGENT_NAME)
    agent_id = f"{AGENT_NAME}:{agent.versions.latest.version}"
    evaluation = openai_client.evals.create(
        name="m7-trace-evaluation",
        data_source_config={"type": "azure_ai_source", "scenario": "traces"},
        testing_criteria=[
            {
                "type": "azure_ai_evaluator",
                "name": "intent_resolution",
                "evaluator_name": "builtin.intent_resolution",
                "initialization_parameters": {"deployment_name": os.environ["MODEL_DEPLOYMENT_NAME"]},
                "data_mapping": {"query": "{{item.query}}", "response": "{{item.response}}"},
            },
            {
                "type": "azure_ai_evaluator",
                "name": "task_adherence",
                "evaluator_name": "builtin.task_adherence",
                "initialization_parameters": {"deployment_name": os.environ["MODEL_DEPLOYMENT_NAME"]},
                "data_mapping": {"query": "{{item.query}}", "response": "{{item.response}}"},
            },
        ],
    )
    run = openai_client.evals.runs.create(
        eval_id=evaluation.id,
        name="m7-agent-traces",
        data_source={"type": "azure_ai_traces", "agent_id": agent_id, "max_traces": 20, "lookback_hours": 2},
    )
    while run.status not in ("completed", "failed", "canceled"):
        time.sleep(10)
        run = openai_client.evals.runs.retrieve(run_id=run.id, eval_id=evaluation.id)
    print(f"agent {agent_id}")
    print(f"run {run.status}  {run.result_counts}")
    for output_item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id):
        verdicts = " ".join(f"{result.name}={'pass' if result.passed else 'fail'}" for result in output_item.results)
        item = output_item.datasource_item
        print(f"{item['trace_id'][:8]}  {item['response'][:46]!r:50}  {verdicts}")
