import os
from contextlib import contextmanager
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from openai import OpenAI

load_dotenv(override=True)


@contextmanager
def connect():
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):
        yield openai_client


@contextmanager
def connect_embeddings():
    credential = DefaultAzureCredential()
    resource_url = os.environ["PROJECT_ENDPOINT"].split("/api/projects")[0]
    with OpenAI(
        base_url=f"{resource_url}/openai/v1/",
        api_key=get_bearer_token_provider(credential, "https://ai.azure.com/.default"),
    ) as embeddings_client:
        yield embeddings_client
