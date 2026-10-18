from common import connect

job_id = open("job_id.txt").read()

with connect() as (project_client, openai_client):
    job = project_client.agents.get_optimization_job(job_id=job_id)
    summary = job.result.candidate_summary
    print(f"job {job.status}, termination {job.result.termination_reason}")
    print(f"{'candidate':12} {'score':>7} {'delta':>7} {'avg_tokens':>11} {'latency_ms':>11}")
    for candidate in sorted(project_client.agents.list_optimization_candidates(job_id=job_id), key=lambda item: item.name):
        evaluation = candidate.evaluation
        marker = " *" if candidate.candidate_id == summary.best_id else ""
        print(
            f"{candidate.name:12} {evaluation.score:7.3f} {evaluation.score - summary.baseline_score:+7.3f} "
            f"{evaluation.avg_tokens:11.0f} {evaluation.avg_latency_ms.total_seconds() * 1000:11.0f}{marker}"
        )
    for usage in job.result.token_usage:
        print(f"{usage.stage.value:13} {usage.model:10} {usage.total_tokens:>8} tokens")
