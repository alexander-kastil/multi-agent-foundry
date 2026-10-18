import json
import os
import sys

import httpx
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

url = f"http://127.0.0.1:{os.getenv('AGUI_PORT', '8888')}/"


def run(body: dict) -> list[dict]:
    events = []
    with httpx.stream("POST", url, json=body, headers={"Accept": "text/event-stream"}, timeout=120) as response:
        for line in response.iter_lines():
            if line.startswith("data:"):
                events.append(json.loads(line[5:]))
    return events


first = run({"threadId": "approval-1", "runId": "run-1", "messages": [{"id": "1", "role": "user", "content": sys.argv[1]}]})
interrupt = [e for e in first if e["type"] == "RUN_FINISHED"][-1]["outcome"]["interrupts"][0]
print(f"paused: {interrupt['message']}")

snapshot = [e for e in first if e["type"] == "MESSAGES_SNAPSHOT"][-1]["messages"]
decision = {"interruptId": interrupt["id"], "status": "resolved", "payload": {"approved": sys.argv[2] == "approve"}}
second = run({"threadId": "approval-1", "runId": "run-2", "messages": snapshot, "resume": [decision]})

for event in second:
    if event["type"] == "TOOL_CALL_RESULT":
        print(f"tool result: {event['content']}")
print(f"agent: {''.join(e['delta'] for e in second if e['type'] == 'TEXT_MESSAGE_CONTENT')}")
