import json
from common import QUESTIONS, ask_agent, connect, create_agent_version

with connect() as (project_client, openai_client):
    agent = create_agent_version(project_client)
    print(f"agent {agent.name} version {agent.version}")
    response_ids = []
    for question in QUESTIONS:
        response = ask_agent(openai_client, agent, question)
        response_ids.append(response.id)
        print(f"{response.id[:18]}  {response.output_text[:90]!r}")

with open("response_ids.json", "w") as file:
    json.dump(response_ids, file)
