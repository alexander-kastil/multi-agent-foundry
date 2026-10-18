from common import AGENT_NAME, connect, score_version

job_id = open("job_id.txt").read()

with connect() as (project_client, openai_client):
    job = project_client.agents.get_optimization_job(job_id=job_id)
    best_id = job.result.candidate_summary.best_id
    best = next(
        candidate
        for candidate in project_client.agents.list_optimization_candidates(job_id=job_id, expand=["mutations"])
        if candidate.candidate_id == best_id
    )
    for mutation in best.as_dict()["output"]["mutations"]:
        print(f"{mutation['type']} mutation:")
        print(mutation["value"])

    promoted = project_client.agents.promote_optimization_candidate(job_id=job_id, candidate_id=best_id)
    print(f"promoted {promoted.name} to a new version of {AGENT_NAME}")

    versions = project_client.agents.get(AGENT_NAME).versions.latest.version
    score, count = score_version(project_client, openai_client, versions)
    print(f"version {versions}: mean f1 {score:.2f} on {count} tickets")
