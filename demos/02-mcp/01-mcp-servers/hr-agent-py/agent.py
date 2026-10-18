import asyncio
import os
import sys

from agent_framework import Agent, MCPStreamableHTTPTool
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv()
sys.stdout.reconfigure(encoding="utf-8")

SERVER_URL = os.getenv("MCP_SERVER_URL", "http://127.0.0.1:47003/mcp")


async def main():
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=DefaultAzureCredential(),
    )
    hr = MCPStreamableHTTPTool(name="hr", url=SERVER_URL, approval_mode="never_require")
    async with Agent(client=client, instructions="You are an HR assistant. Use the hr tools for every question about staff.", tools=[hr]) as agent:
        result = await agent.run("Who on the team has baking skills, and which languages do they speak?")
        print(result.text)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nInterrupted.")
