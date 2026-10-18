from common import AGENT_NAME, EVALUATION_NAMES, RULE_ID, connect

with connect() as (project_client, openai_client):
    project_client.evaluation_rules.delete(id=RULE_ID)
    print(f"deleted rule {RULE_ID}")
    for evaluation in openai_client.evals.list():
        if evaluation.name in EVALUATION_NAMES:
            openai_client.evals.delete(eval_id=evaluation.id)
            print(f"deleted evaluation {evaluation.name}")
    project_client.beta.evaluation_taxonomies.delete(name=AGENT_NAME)
    print(f"deleted taxonomy {AGENT_NAME}")
    project_client.agents.delete(agent_name=AGENT_NAME)
    print(f"deleted agent {AGENT_NAME}")
