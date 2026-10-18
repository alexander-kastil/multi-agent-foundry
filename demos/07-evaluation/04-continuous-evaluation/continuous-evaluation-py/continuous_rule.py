import os
import time
from azure.ai.projects.models import (
    ContinuousEvaluationRuleAction,
    EvaluationRule,
    EvaluationRuleEventType,
    EvaluationRuleFilter,
)
from common import AGENT_NAME, QUESTIONS, RULE_ID, ask_agent, connect

with connect() as (project_client, openai_client):
    agent = project_client.agents.get(AGENT_NAME)
    latest = project_client.agents.get_version(agent_name=AGENT_NAME, agent_version=agent.versions.latest.version)

    evaluation = openai_client.evals.create(
        name="m7-continuous-evaluation",
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
    print(f"evaluation {evaluation.id}")

    rule = project_client.evaluation_rules.create_or_update(
        id=RULE_ID,
        evaluation_rule=EvaluationRule(
            display_name="m7 continuous evaluation",
            description="Evaluate every completed response of the support agent",
            action=ContinuousEvaluationRuleAction(eval_id=evaluation.id, max_hourly_runs=100),
            event_type=EvaluationRuleEventType.RESPONSE_COMPLETED,
            filter=EvaluationRuleFilter(agent_name=AGENT_NAME),
            enabled=True,
        ),
    )
    print(f"rule {rule.id} enabled={rule.enabled} event={rule.event_type}")

    for question in QUESTIONS:
        ask_agent(openai_client, latest, question)
    print(f"sent {len(QUESTIONS)} questions to {AGENT_NAME} version {latest.version}")

    for _ in range(30):
        runs = list(openai_client.evals.runs.list(eval_id=evaluation.id))
        if runs and all(run.status in ("completed", "failed", "canceled") for run in runs):
            break
        time.sleep(20)
    for run in runs:
        print(f"run {run.id[:24]}  {run.status}  {run.result_counts}")
