import os
import sys
from pathlib import Path

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

with AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential(), allow_preview=True) as project:
    version = project.agents.get_version("m5-support-router", sys.argv[1] if len(sys.argv) > 1 else "1")
    Path("migration/exported.workflow.yaml").write_text(version.definition.workflow, encoding="utf-8")
    print(f"exported {version.name} version {version.version}, {len(version.definition.workflow.splitlines())} lines")
