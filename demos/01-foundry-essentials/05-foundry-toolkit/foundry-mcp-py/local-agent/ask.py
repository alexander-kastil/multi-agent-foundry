import os
import sys
from dotenv import load_dotenv
from openai import OpenAI

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

client = OpenAI(base_url="http://localhost:8088", api_key="local")

response = client.responses.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    input=sys.argv[1],
)

print(f"status : {response.status}")
print(f"items  : {[item.type for item in response.output]}")
print(f"answer : {response.output_text}")
