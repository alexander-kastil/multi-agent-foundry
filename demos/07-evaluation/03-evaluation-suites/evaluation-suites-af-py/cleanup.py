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
        if evaluation.name.startswith("m7-returns-agent-"):
            openai_client.evals.delete(eval_id=evaluation.id)
            print(f"deleted evaluation {evaluation.name}")
