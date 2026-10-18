import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    for evaluation in openai_client.evals.list():
        if evaluation.name == "m7-agent-evaluators":
            openai_client.evals.delete(eval_id=evaluation.id)
            print(f"deleted evaluation {evaluation.id}")
    for dataset in project_client.datasets.list_versions(name="m7-agent-trajectories"):
        project_client.datasets.delete(name=dataset.name, version=dataset.version)
        print(f"deleted dataset {dataset.name} {dataset.version}")
