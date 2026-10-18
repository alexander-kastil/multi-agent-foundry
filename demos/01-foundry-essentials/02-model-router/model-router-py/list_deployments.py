import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)

for deployment in project.deployments.list():
    if deployment.model_name == "model-router":
        print(f"{deployment.name:<22} {deployment.model_name} {deployment.model_version} {deployment.sku.name} capacity={deployment.sku.capacity}")
