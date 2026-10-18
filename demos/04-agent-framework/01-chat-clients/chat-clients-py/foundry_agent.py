import asyncio
import os
import sys

from agent_framework.foundry import FoundryAgent
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    agent = FoundryAgent(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        agent_name=os.environ["FOUNDRY_AGENT_NAME"],
        agent_version="1",
        credential=DefaultAzureCredential(),
    )

    result = await agent.run("How do I return a tent?")
    print(result.text)

    try:
        await agent.run(
            "How do I return a tent?",
            options={"instructions": "Answer only in French."},
        )
    except Exception as error:
        message = error.__cause__.body["message"]
        print(f"per-run instructions rejected: {message}")


asyncio.run(main())
