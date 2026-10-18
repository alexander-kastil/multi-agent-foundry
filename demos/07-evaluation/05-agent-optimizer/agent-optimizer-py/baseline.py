from common import AGENT_NAME, BASELINE_INSTRUCTIONS, connect, create_agent_version, score_version

with connect() as (project_client, openai_client):
    agent = create_agent_version(project_client, BASELINE_INSTRUCTIONS)
    print(f"agent {AGENT_NAME} version {agent.version}")
    score, count = score_version(project_client, openai_client, agent.version)
    print(f"baseline version {agent.version}: mean f1 {score:.2f} on {count} tickets")
