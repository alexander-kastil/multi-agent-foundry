import json
import os
import time
from azure.ai.projects.models import (
    AgentTaxonomyInput,
    AzureAIAgentTarget,
    AzureAIDataSourceConfig,
    EvaluationTaxonomy,
    RedTeamEvalRunDataSource,
    RiskCategory,
    TestingCriterionAzureAIEvaluator,
)
from common import AGENT_NAME, connect, create_agent_version

with connect() as (project_client, openai_client):
    agent = create_agent_version(project_client)
    target = AzureAIAgentTarget(name=AGENT_NAME, version=agent.version)

    evaluation = openai_client.evals.create(
        name="m7-red-team-evaluation",
        data_source_config=AzureAIDataSourceConfig(type="azure_ai_source", scenario="red_team"),
        testing_criteria=[
            TestingCriterionAzureAIEvaluator(
                type="azure_ai_evaluator", name="Prohibited Actions", evaluator_name="builtin.prohibited_actions", evaluator_version="1"
            ),
            TestingCriterionAzureAIEvaluator(
                type="azure_ai_evaluator",
                name="Task Adherence",
                evaluator_name="builtin.task_adherence",
                evaluator_version="1",
                initialization_parameters={"deployment_name": os.environ["MODEL_DEPLOYMENT_NAME"]},
            ),
            TestingCriterionAzureAIEvaluator(
                type="azure_ai_evaluator", name="Sensitive Data Leakage", evaluator_name="builtin.sensitive_data_leakage", evaluator_version="1"
            ),
        ],
    )
    taxonomy = project_client.beta.evaluation_taxonomies.create(
        name=AGENT_NAME,
        taxonomy=EvaluationTaxonomy(
            description="Taxonomy for red teaming the support agent",
            taxonomy_input=AgentTaxonomyInput(risk_categories=[RiskCategory.PROHIBITED_ACTIONS], target=target),
        ),
    )
    run = openai_client.evals.runs.create(
        eval_id=evaluation.id,
        name="m7-red-team-run",
        data_source=RedTeamEvalRunDataSource(
            type="azure_ai_red_team",
            item_generation_params={
                "type": "red_team_taxonomy",
                "attack_strategies": ["Flip", "Base64"],
                "num_turns": 3,
                "source": {"type": "file_id", "id": taxonomy.id},
            },
            target=target.as_dict(),
        ),
    )
    print(f"red team run {run.id} started against {AGENT_NAME} version {agent.version}")
    started = time.time()
    while run.status not in ("completed", "failed", "canceled"):
        time.sleep(15)
        run = openai_client.evals.runs.retrieve(run_id=run.id, eval_id=evaluation.id)
        print(f"  {int(time.time() - started)}s {run.status}")
    print(f"run {run.status}  {run.result_counts}")
    items = [item.model_dump() for item in openai_client.evals.runs.output_items.list(run_id=run.id, eval_id=evaluation.id)]

with open("red_team_items.json", "w") as file:
    json.dump(items, file, indent=2, default=str)
print(f"{len(items)} output items written to red_team_items.json")
