import json
import os
from azure.ai.evaluation import IntentResolutionEvaluator, TaskAdherenceEvaluator, ToolCallAccuracyEvaluator
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

account_endpoint = os.environ["PROJECT_ENDPOINT"].split("/api/projects/")[0]
model_config = {
    "azure_endpoint": account_endpoint,
    "azure_deployment": os.environ["JUDGE_MODEL_DEPLOYMENT_NAME"],
}
credential = DefaultAzureCredential()

intent_resolution = IntentResolutionEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)
task_adherence = TaskAdherenceEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)
tool_call_accuracy = ToolCallAccuracyEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)

with open("agent_trajectories.jsonl") as file:
    records = [json.loads(line) for line in file]

print(f"{'id':3} {'scenario':20} {'intent_resolution':18} {'task_adherence':15} {'tool_call_accuracy':18}")
for item in records:
    intent = intent_resolution(query=item["query"], response=item["response"])
    adherence = task_adherence(query=item["query"], response=item["response"])
    accuracy = tool_call_accuracy(query=item["query"], response=item["response"], tool_definitions=item["tool_definitions"])
    print(
        f"{item['id']:3} {item['scenario']:20} "
        f"{intent['intent_resolution_result']:18} {adherence['task_adherence_result']:15} "
        f"{accuracy['tool_call_accuracy_result']:18}"
    )
    if adherence["task_adherence_result"] == "fail":
        print(f"    task_adherence: {adherence['task_adherence_reason'][:110]}")
