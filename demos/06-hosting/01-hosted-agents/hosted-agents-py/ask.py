import os
import sys

from openai import OpenAI

sys.stdout.reconfigure(encoding="utf-8")

client = OpenAI(base_url=os.getenv("AGENT_URL", "http://localhost:8088"), api_key="not-needed")
response = client.responses.create(input=sys.argv[1])
print(response.output_text)
