import os
import time
from azure.ai.projects.models import (
    AgentOptimizationCandidateSearchConfiguration,
    AgentOptimizationConfiguration,
    AgentOptimizationEvaluationConfiguration,
    AgentOptimizationEvaluator,
    AgentOptimizationFoundryAgentTargetConfiguration,
    AgentOptimizationJob,
    AgentOptimizationModelConfiguration,
    AgentOptimizationSpace,
    AgentOptimizationTargetCompletionEvaluationSet,
    AgentOptimizationTargetCompletionInlineDataSource,
    AgentOptimizationTargetCompletionTestCase,
    EvaluationModelConfiguration,
    JobStatus,
    TargetAttribute,
)
from common import AGENT_NAME, connect, load_tickets

with connect() as (project_client, openai_client):
    agent = project_client.agents.get(AGENT_NAME)
    version = agent.versions.latest.version
    test_cases = [AgentOptimizationTargetCompletionTestCase(query=row["query"], ground_truth=row["ground_truth"]) for row in load_tickets()]

    job = AgentOptimizationJob(
        display_name="m7 ticket triage",
        target_configuration=AgentOptimizationFoundryAgentTargetConfiguration(name=AGENT_NAME, version=version),
        optimization_model_configuration=AgentOptimizationModelConfiguration(model=os.environ["OPTIMIZATION_MODEL_DEPLOYMENT_NAME"]),
        optimization_configuration=AgentOptimizationConfiguration(
            evaluation_configuration=AgentOptimizationEvaluationConfiguration(
                training_set=AgentOptimizationTargetCompletionEvaluationSet(
                    source=AgentOptimizationTargetCompletionInlineDataSource(test_cases=test_cases)
                ),
                evaluators=[AgentOptimizationEvaluator(name="builtin.f1_score")],
                evaluation_model=EvaluationModelConfiguration(model=os.environ["EVAL_MODEL_DEPLOYMENT_NAME"]),
                max_concurrent_agent_runs=4,
            ),
            candidate_search_configuration=AgentOptimizationCandidateSearchConfiguration(max_candidates=3),
            agent_optimization_space=AgentOptimizationSpace(target_attributes=[TargetAttribute.INSTRUCTIONS]),
        ),
    )
    poller = project_client.agents.begin_create_optimization_job(job=job, polling=False)
    job_id = poller.details.get("job_id")
    print(f"optimization job {job_id}")
    with open("job_id.txt", "w") as file:
        file.write(job_id)

    terminal = {JobStatus.SUCCEEDED, JobStatus.FAILED, JobStatus.CANCELLED}
    current = project_client.agents.get_optimization_job(job_id=job_id)
    while current.status not in terminal:
        print(f"  status {current.status}")
        time.sleep(30)
        current = project_client.agents.get_optimization_job(job_id=job_id)
    print(f"  status {current.status}")
    if current.status == JobStatus.FAILED:
        print(current.error.message if current.error else "no error message")
        raise SystemExit(1)

    summary = current.result.candidate_summary
    print(f"baseline {summary.baseline_score:.4f}  best {summary.best_score:.4f}  best candidate {summary.best_id}")
    for candidate in project_client.agents.list_optimization_candidates(job_id=job_id):
        score = candidate.evaluation.score if candidate.evaluation else None
        print(f"{candidate.name}  {candidate.candidate_id}  {candidate.status}  score={score}")
