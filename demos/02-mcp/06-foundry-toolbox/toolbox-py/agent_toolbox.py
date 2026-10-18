import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient, FoundryToolbox
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()
sys.stdout.reconfigure(encoding="utf-8")

TOOLBOX_URL = f"{os.environ['PROJECT_ENDPOINT']}/toolboxes/{os.environ['TOOLBOX_NAME']}/mcp?api-version=v1"


async def main():
    credential = DefaultAzureCredential()
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=credential,
    )
    toolbox = FoundryToolbox(credential, url=TOOLBOX_URL)
    async with Agent(
        client=client,
        instructions="Answer from Microsoft Learn and keep the snippets collection current. Keep answers under 60 words.",
        tools=[toolbox],
    ) as agent:
        first = await agent.run("Look up the CLI command that sets the project endpoint for azd ai, then save it as a snippet named azd-project.")
        print(first.text)
        second = await agent.run("What does the snippet named azd-project contain?")
        print(second.text)


try:
    asyncio.run(main())
except KeyboardInterrupt:
    print("\nInterrupted.")
