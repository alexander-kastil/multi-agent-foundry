import os
import sys
import time
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)
openai = project.get_openai_client()

started = time.perf_counter()
stream = openai.responses.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    input="Explain in three sentences why streaming improves perceived latency.",
    stream=True,
)

seen = []
first_token = None
for event in stream:
    if event.type not in seen:
        seen.append(event.type)
    if event.type == "response.output_text.delta":
        if first_token is None:
            first_token = time.perf_counter() - started
        print(event.delta, end="", flush=True)
    if event.type == "response.completed":
        total = time.perf_counter() - started
        usage = event.response.usage

print()
print()
print(f"event types : {seen}")
print(f"first token : {first_token:.2f}s")
print(f"completed   : {total:.2f}s ({usage.output_tokens} output tokens)")
