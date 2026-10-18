import os
import sys
import time

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())

if len(sys.argv) > 1 and sys.argv[1] == "dispatch":
    result = project.beta.routines.dispatch(
        routine_name="m6-weekday-digest",
        payload={"type": "invoke_agent_responses_api", "input": "Run the digest now for a test."},
    )
    print(f"dispatch_id: {result.dispatch_id}")
    time.sleep(20)

for routine in project.beta.routines.list():
    print(f"{routine.name}  enabled={routine.enabled}  triggers={list(routine.triggers.keys())}")
    for run in project.beta.routines.list_runs(routine.name):
        print(f"  {run.id}  phase={run.phase.value}  source={run.attempt_source.value}  started={run.started_at}")
        print(f"    dispatch={run.dispatch_id}  response={run.response_id}")
