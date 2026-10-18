from common import AGENT_NAME, DATASET_NAME, EVALUATION_FEATURES, EVALUATOR_NAME, TOOL_AGENT_NAME, connect

with connect() as (project_client, openai_client):
    for job in project_client.agents.list_optimization_jobs():
        project_client.agents.delete_optimization_job(job_id=job.id)
        print(f"deleted optimization job {job.id}")
    for evaluation in openai_client.evals.list():
        if evaluation.name.startswith("m7-triage-"):
            openai_client.evals.delete(eval_id=evaluation.id)
            print(f"deleted evaluation {evaluation.name}")
    for dataset in project_client.datasets.list_versions(name=DATASET_NAME):
        project_client.datasets.delete(name=dataset.name, version=dataset.version)
        print(f"deleted dataset {dataset.name} {dataset.version}")
    for version in project_client.evaluators.list_versions(name=EVALUATOR_NAME, headers=EVALUATION_FEATURES):
        project_client.evaluators.delete_version(name=EVALUATOR_NAME, version=version.version, headers=EVALUATION_FEATURES)
        print(f"deleted evaluator {EVALUATOR_NAME} {version.version}")
    existing = {agent.name for agent in project_client.agents.list()}
    for name in (AGENT_NAME, TOOL_AGENT_NAME):
        if name in existing:
            project_client.agents.delete(agent_name=name)
            print(f"deleted agent {name}")
