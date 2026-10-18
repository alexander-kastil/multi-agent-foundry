import asyncio
import os

from azure.ai.projects.aio import AIProjectClient
from azure.identity.aio import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)


async def main() -> None:
    async with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project,
    ):
        await project.beta.memory_stores.delete(os.environ["MEMORY_STORE_NAME"])
        print(f"deleted memory store {os.environ['MEMORY_STORE_NAME']}")


asyncio.run(main())
