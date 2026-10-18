import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework.openai import OpenAIChatClient, OpenAIChatCompletionClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

INSTRUCTIONS = "You are a trail guide for Contoso Outdoor. Answer in two sentences."
QUESTION = "What should I pack for a day hike in the Alps?"


async def main() -> None:
    credential = DefaultAzureCredential()
    model = os.environ["MODEL_DEPLOYMENT_NAME"]
    azure_endpoint = os.environ["AZURE_OPENAI_ENDPOINT"]

    for client in (
        OpenAIChatClient(model=model, azure_endpoint=azure_endpoint, credential=credential),
        OpenAIChatCompletionClient(model=model, azure_endpoint=azure_endpoint, credential=credential),
    ):
        agent = Agent(client=client, name="OutdoorGuide", instructions=INSTRUCTIONS)
        result = await agent.run(QUESTION)
        print(f"[{type(client).__name__}] {result.text}\n")


asyncio.run(main())
