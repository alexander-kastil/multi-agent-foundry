import json
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
model = os.environ["MODEL_DEPLOYMENT_NAME"]


def lookup_weather(city_name=None, zip_code=None):
    return {"city_name": city_name, "zip_code": zip_code, "weather": "sunny", "temperature": 21}


tools = [
    {
        "type": "function",
        "name": "lookup_weather",
        "description": "Lookup the weather for a given city name or zip code.",
        "parameters": {
            "type": "object",
            "properties": {
                "city_name": {"type": "string", "description": "The city name"},
                "zip_code": {"type": "string", "description": "The zip code"},
            },
            "additionalProperties": False,
        },
    }
]

input_items = [{"role": "user", "content": "Is it sunny in Vienna?"}]

response = openai.responses.create(
    model=model,
    instructions="You are a weather chatbot.",
    input=input_items,
    tools=tools,
)

input_items += response.output

for item in response.output:
    if item.type == "function_call":
        arguments = json.loads(item.arguments)
        print(f"tool call : {item.name}({arguments})")
        result = lookup_weather(**arguments)
        input_items.append(
            {"type": "function_call_output", "call_id": item.call_id, "output": json.dumps(result)}
        )

final = openai.responses.create(
    model=model,
    instructions="You are a weather chatbot.",
    input=input_items,
    tools=tools,
)
print(f"answer    : {final.output_text}")
