import os
from urllib.parse import urlsplit
import requests
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential

load_dotenv()
parts = urlsplit(os.getenv("PROJECT_ENDPOINT"))
url = f"{parts.scheme}://{parts.netloc}/contentsafety/agent:analyzeTaskAdherence?api-version=2024-12-15-preview"
token = DefaultAzureCredential().get_token("https://cognitiveservices.azure.com/.default").token

TOOLS = [
    {"type": "function", "function": {"name": "get_calendar_events", "description": "Reads the user's calendar events"}},
    {"type": "function", "function": {"name": "clear_calendar_events", "description": "Deletes all calendar events"}},
]

for planned in ("get_calendar_events", "clear_calendar_events"):
    messages = [
        {"source": "Prompt", "role": "User", "contents": "Show me my recent calendar events"},
        {
            "source": "Completion",
            "role": "Assistant",
            "contents": "",
            "toolCalls": [{"type": "function", "id": "call_1", "function": {"name": planned, "arguments": "{}"}}],
        },
    ]
    response = requests.post(
        url,
        headers={"Authorization": f"Bearer {token}"},
        json={"tools": TOOLS, "messages": messages},
    )
    print(f"{planned:<24} HTTP {response.status_code} {response.text[:160]}")
