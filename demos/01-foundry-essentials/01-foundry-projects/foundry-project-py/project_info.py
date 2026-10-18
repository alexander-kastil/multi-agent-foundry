import os
from urllib.parse import urlparse

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

endpoint = os.environ["PROJECT_ENDPOINT"]
model = os.environ["MODEL_DEPLOYMENT_NAME"]

parts = urlparse(endpoint)
print("--- Endpoint ---")
print("resource:", parts.hostname.split(".")[0])
print("project :", parts.path.rstrip("/").split("/")[-1])
print("host    :", parts.hostname)

project = AIProjectClient(endpoint=endpoint, credential=DefaultAzureCredential())

print("\n--- Deployments ---")
for deployment in project.deployments.list():
    print(f"{deployment.name:<24} {deployment.model_name:<24} {deployment.model_version}")

print("\n--- Connections ---")
connections = list(project.connections.list())
print(len(connections), "connection(s)")
for connection in connections:
    print(connection.name, connection.type)

print("\n--- First call ---")
openai = project.get_openai_client()
print("client base_url:", openai.base_url)
response = openai.responses.create(model=model, input="Name the Azure resource type behind a Foundry resource, in one line.")
print(response.output_text)
