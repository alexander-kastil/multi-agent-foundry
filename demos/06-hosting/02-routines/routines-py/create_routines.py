import os
from datetime import datetime, timedelta, timezone

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
agent_name = os.environ["AGENT_NAME"]

schedule = project.beta.routines.create_or_update(
    routine_name="m6-weekday-digest",
    description="Weekday morning support digest.",
    enabled=False,
    triggers={
        "weekday-morning": {
            "type": "schedule",
            "cron_expression": "0 7 * * 1-5",
            "time_zone": "Europe/Vienna",
        }
    },
    action={
        "type": "invoke_agent_responses_api",
        "agent_name": agent_name,
        "input": "Write the support digest for the last 24 hours.",
    },
)
print(f"routine {schedule.name} enabled={schedule.enabled}")

fire_at = datetime.now(timezone.utc) + timedelta(minutes=2)
timer = project.beta.routines.create_or_update(
    routine_name="m6-one-shot",
    description="One-time digest two minutes from now.",
    enabled=True,
    triggers={"once": {"type": "timer", "at": fire_at}},
    action={
        "type": "invoke_agent_responses_api",
        "agent_name": agent_name,
        "input": "Write the one-time support digest.",
    },
)
print(f"routine {timer.name} enabled={timer.enabled} fires at {fire_at.isoformat(timespec='seconds')}")
