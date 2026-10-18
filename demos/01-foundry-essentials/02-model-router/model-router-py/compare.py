import sys
import time
import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

PROMPTS = {
    "lookup": "What is the capital of Austria?",
    "code": "Write a Python function that merges overlapping intervals and explain its time complexity.",
    "reasoning": "A bat and a ball cost 1.10 in total and the bat costs 1.00 more than the ball. Solve it with algebra and explain why intuition fails.",
}

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)
openai = project.get_openai_client()

print(f"{'deployment':<22} {'prompt':<10} {'served by':<32} {'sec':>5} {'tokens':>7}")
for deployment in sys.argv[1:]:
    for label, prompt in PROMPTS.items():
        started = time.perf_counter()
        response = openai.responses.create(model=deployment, input=prompt)
        elapsed = time.perf_counter() - started
        print(f"{deployment:<22} {label:<10} {response.model:<32} {elapsed:>5.1f} {response.usage.total_tokens:>7}")
