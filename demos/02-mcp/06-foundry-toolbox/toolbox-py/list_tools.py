import asyncio
import os
import sys

from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client

load_dotenv()

VERSION = sys.argv[1] if len(sys.argv) > 1 else None
BASE = f"{os.environ['PROJECT_ENDPOINT']}/toolboxes/{os.environ['TOOLBOX_NAME']}"
URL = f"{BASE}/versions/{VERSION}/mcp?api-version=v1" if VERSION else f"{BASE}/mcp?api-version=v1"


async def main():
    token = DefaultAzureCredential().get_token("https://ai.azure.com/.default").token
    async with streamablehttp_client(URL, headers={"Authorization": f"Bearer {token}"}) as (read, write, _):
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = await session.list_tools()
            print(f"{URL.split('/api/projects/')[1]}")
            for tool in tools.tools:
                print(f"  {tool.name}: {(tool.description or '')[:70]}")
            result = await session.call_tool("snippets___hello_mcp", {})
            print("snippets___hello_mcp ->", result.content[0].text)


asyncio.run(main())
