import sys
import os

from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from dotenv import load_dotenv
from openai import OpenAI

sys.stdout.reconfigure(encoding="utf-8")
load_dotenv(override=True)

token_provider = get_bearer_token_provider(DefaultAzureCredential(), "https://ai.azure.com/.default")
client = OpenAI(base_url=os.environ["OPENAI_ENDPOINT"], api_key=token_provider)

response = client.responses.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    input="Which endpoint am I calling? Answer in five words.",
)
print(response.output_text)
