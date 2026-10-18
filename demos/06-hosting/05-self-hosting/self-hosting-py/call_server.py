import os
import sys

from openai import OpenAI

sys.stdout.reconfigure(encoding="utf-8")

client = OpenAI(base_url=os.getenv("SERVER_URL", "http://localhost:8000"), api_key="not-needed")

first = client.responses.create(model="support-desk", input=sys.argv[1])
print(first.output_text)

second = client.responses.create(model="support-desk", input=sys.argv[2], previous_response_id=first.id)
print(second.output_text)
