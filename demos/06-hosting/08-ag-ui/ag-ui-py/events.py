import json
import os
import sys
from collections import Counter

import httpx
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

PATHS = {"chat": "/", "workflow": "/workflow"}

url = f"http://127.0.0.1:{os.getenv('AGUI_PORT', '8888')}{PATHS[sys.argv[1]]}"
body = {"threadId": "t1", "runId": "r1", "messages": [{"id": "1", "role": "user", "content": sys.argv[2]}]}

counts = Counter()
text = ""
with httpx.stream("POST", url, json=body, headers={"Accept": "text/event-stream"}, timeout=120) as response:
    for line in response.iter_lines():
        if not line.startswith("data:"):
            continue
        event = json.loads(line[5:])
        counts[event["type"]] += 1
        if event["type"] in ("RUN_STARTED", "STEP_STARTED", "STEP_FINISHED", "TOOL_CALL_START", "RUN_FINISHED", "RUN_ERROR"):
            print(event["type"], event.get("stepName") or event.get("toolCallName") or "")
        if event["type"] == "TEXT_MESSAGE_CONTENT":
            text += event["delta"]
        if event["type"] == "RUN_FINISHED":
            print("outcome:", event.get("outcome"))

print(f"text: {text}")
print(dict(counts))
