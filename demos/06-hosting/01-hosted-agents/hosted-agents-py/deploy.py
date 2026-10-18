import hashlib
import io
import os
import time
import zipfile
from pathlib import Path

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import CodeConfiguration, HostedAgentDefinition, ProtocolVersionRecord
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

AGENT_NAME = "m6-support-desk"

buffer = io.BytesIO()
with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as archive:
    for name in ("main.py", "requirements.txt"):
        archive.write(Path(name), name)
code = buffer.getvalue()
digest = hashlib.sha256(code).hexdigest()

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())

created = project.agents.create_version_from_code(
    agent_name=AGENT_NAME,
    definition=HostedAgentDefinition(
        cpu="1",
        memory="2Gi",
        code_configuration=CodeConfiguration(
            runtime="python_3_13",
            entry_point=["python", "main.py"],
            dependency_resolution="remote_build",
        ),
        protocol_versions=[ProtocolVersionRecord(protocol="responses", version="2.0.0")],
        environment_variables={"AZURE_AI_MODEL_DEPLOYMENT_NAME": os.environ["MODEL_DEPLOYMENT_NAME"]},
    ),
    code=("agent-code.zip", code, "application/zip"),
    code_zip_sha256=digest,
    description="Contoso support desk hosted agent",
)
print(f"created {created.name} version {created.version}")

while True:
    version = project.agents.get_version(agent_name=AGENT_NAME, agent_version=created.version)
    print(f"status: {version['status']}")
    if version["status"] == "active":
        break
    if version["status"] == "failed":
        raise SystemExit(version.get("error"))
    time.sleep(10)
