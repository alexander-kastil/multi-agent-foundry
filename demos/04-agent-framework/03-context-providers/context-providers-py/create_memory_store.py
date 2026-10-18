import asyncio
import os

from azure.ai.projects.aio import AIProjectClient
from azure.ai.projects.models import MemoryStoreDefaultDefinition, MemoryStoreDefaultOptions
from azure.identity.aio import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)


async def main() -> None:
    async with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=credential) as project,
    ):
        store = await project.beta.memory_stores.create(
            name=os.environ["MEMORY_STORE_NAME"],
            description="Long-term memory for the Contoso Outdoor trail guide",
            definition=MemoryStoreDefaultDefinition(
                chat_model=os.environ["MODEL_DEPLOYMENT_NAME"],
                embedding_model=os.environ["EMBEDDING_MODEL_DEPLOYMENT_NAME"],
                options=MemoryStoreDefaultOptions(
                    chat_summary_enabled=False,
                    user_profile_enabled=True,
                    user_profile_details="Trail preferences, equipment and companions. Avoid sensitive personal data.",
                ),
            ),
        )
        print(f"created memory store {store.name}")


asyncio.run(main())
