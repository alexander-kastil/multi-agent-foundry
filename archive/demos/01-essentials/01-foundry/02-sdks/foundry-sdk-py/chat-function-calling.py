import json
import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()

project_client = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential()
)
client = project_client.get_openai_client()
MODEL_NAME = os.environ["MODEL"]

def lookup_weather(city_name=None, zip_code=None):
    """Lookup the weather for a given city name or zip code."""
    return {
        "city_name": city_name,
        "zip_code": zip_code,
        "weather": "sunny",
        "temperature": 75,
    }

tools = [
    {
        "type": "function",
        "function": {
            "name": "lookup_weather",
            "description": "Lookup the weather for a given city name or zip code.",
            "parameters": {
                "type": "object",
                "properties": {
                    "city_name": {
                        "type": "string",
                        "description": "The city name",
                    },
                    "zip_code": {
                        "type": "string",
                        "description": "The zip code",
                    },
                },
                "strict": True,
                "additionalProperties": False,
            },
        },
    }
]

messages = [
    {"role": "system", "content": "You are a weather chatbot."},
    {"role": "user", "content": "Is it sunny in Vienna?"},
]
response = client.chat.completions.create(
    model=MODEL_NAME,
    messages=messages,
    tools=tools,
    tool_choice="auto",
)

print(f"Response from {MODEL_NAME}: \n")

# Now actually call the function as indicated

if response.choices[0].message.tool_calls:
    tool_call = response.choices[0].message.tool_calls[0]
    function_name = tool_call.function.name
    arguments = json.loads(tool_call.function.arguments)

    if function_name == "lookup_weather":
        messages.append(response.choices[0].message)
        result = lookup_weather(**arguments)
        messages.append({"role": "tool", "tool_call_id": tool_call.id, "content": str(result)})
        response = client.chat.completions.create(model=MODEL_NAME, messages=messages, tools=tools)
        print(response.choices[0].message.content)
