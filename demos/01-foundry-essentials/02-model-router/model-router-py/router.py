import os
import time

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

deployments = os.environ["ROUTER_DEPLOYMENTS"].split(",")

prompts = {
    "simple": "What is the capital of Austria?",
    "code": "Write a Python function that merges overlapping intervals and explain its complexity.",
    "reasoning": "A bat and a ball cost 1.10 in total; the bat costs 1.00 more than the ball. Prove the answer with algebra and explain why intuition fails.",
}

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()

print(f"{'deployment':<22}{'prompt':<11}{'routed to':<30}{'seconds':>8}{'tokens':>8}")
for deployment in deployments:
    for label, prompt in prompts.items():
        started = time.time()
        completion = openai.chat.completions.create(
            model=deployment,
            messages=[{"role": "user", "content": prompt}],
        )
        elapsed = time.time() - started
        print(f"{deployment:<22}{label:<11}{completion.model:<30}{elapsed:>8.1f}{completion.usage.total_tokens:>8}")

print("\n--- Responses API ---")
response = openai.responses.create(model=deployments[0], input=prompts["simple"])
print("routed to:", response.model)
print(response.output_text)
