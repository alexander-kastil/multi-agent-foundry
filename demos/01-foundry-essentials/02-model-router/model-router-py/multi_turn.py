import os
import sys
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

messages = [{"role": "system", "content": "You are a concise travel planner."}]
turns = [
    "Plan a two-day food trip to Vienna.",
    "Swap day two for a day trip to Salzburg.",
    "List the packing essentials as five bullet points.",
]

for turn in turns:
    messages.append({"role": "user", "content": turn})
    response = openai.chat.completions.create(
        model=os.environ["MODEL_ROUTER_DEPLOYMENT_NAME"],
        messages=messages,
    )
    answer = response.choices[0].message.content
    messages.append({"role": "assistant", "content": answer})
    print(f"user      : {turn}")
    print(f"served by : {response.model}")
    print(f"answer    : {answer[:120].replace(chr(10), ' ')}...")
    print()
