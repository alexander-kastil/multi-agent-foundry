import os

from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from openai import OpenAI
from azure.ai.projects import AIProjectClient

load_dotenv(override=True)

PROJECT_ENDPOINT = os.environ["PROJECT_ENDPOINT"]
MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]
EMBEDDING_MODEL = os.environ["EMBEDDING_MODEL_DEPLOYMENT_NAME"]
FOUNDRY_ENDPOINT = os.environ["FOUNDRY_ENDPOINT"]
SEARCH_ENDPOINT = os.environ["SEARCH_ENDPOINT"]
PROJECT_RESOURCE_ID = os.environ["PROJECT_RESOURCE_ID"]

API_VERSION = "2026-08-01-preview"
KNOWLEDGE_BASE = "m3-outdoor-kb"
SOURCES = {"m3-products": "m3-products-ks", "m3-policies": "m3-policies-ks"}
CONNECTION = "m3-outdoor-kb-mcp"
AGENT = "m3-outdoor-agent"

credential = DefaultAzureCredential()
project = AIProjectClient(endpoint=PROJECT_ENDPOINT, credential=credential)
openai = project.get_openai_client()
embeddings = OpenAI(
    base_url=f"{FOUNDRY_ENDPOINT}/openai/v1/",
    api_key=get_bearer_token_provider(credential, "https://cognitiveservices.azure.com/.default"),
)
