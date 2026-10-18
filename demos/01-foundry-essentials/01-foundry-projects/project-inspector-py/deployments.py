import os
import sys
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)

for deployment in project.deployments.list():
    print(f"{deployment.name:<28} {deployment.model_name:<28} {deployment.model_version:<12} {deployment.sku.name}")

print()
connections = list(project.connections.list())
print(f"Connections on the project: {len(connections)}")
for connection in connections:
    print(f"{connection.name:<28} {connection.type}")
