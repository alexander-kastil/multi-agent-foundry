import os
import sys
from pathlib import Path

from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    PromptAgentDefinition,
    PromptAgentDefinitionTextOptions,
    TextResponseFormatJsonSchema,
    WorkflowAgentDefinition,
)
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]
CLASSIFIER_SCHEMA = {
    "type": "object",
    "properties": {"category": {"type": "string", "enum": ["billing", "technical", "other"]}},
    "required": ["category"],
    "additionalProperties": False,
}

with AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential(), allow_preview=True) as project:
    project.agents.create_version(
        "m5-classifier",
        definition=PromptAgentDefinition(
            model=MODEL,
            instructions="Classify the customer message as billing, technical or other.",
            text=PromptAgentDefinitionTextOptions(
                format=TextResponseFormatJsonSchema(name="classification", schema=CLASSIFIER_SCHEMA, strict=True)
            ),
        ),
    )
    project.agents.create_version(
        "m5-billing",
        definition=PromptAgentDefinition(model=MODEL, instructions="You answer billing questions in two sentences."),
    )
    project.agents.create_version(
        "m5-technical",
        definition=PromptAgentDefinition(model=MODEL, instructions="You answer technical questions in two sentences."),
    )
    workflow_yaml = Path("migration/portal-workflow.yaml").read_text(encoding="utf-8")
    version = project.agents.create_version("m5-support-router", definition=WorkflowAgentDefinition(workflow=workflow_yaml))
    print(f"published {version.name} version {version.version}")
