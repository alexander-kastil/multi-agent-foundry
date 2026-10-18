import asyncio
import os

import httpx
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from mcp import Client
from mcp.client.streamable_http import streamable_http_client

load_dotenv()
SERVER_URL = os.getenv("MCP_SERVER_URL", "http://localhost:47011/mcp")
API_CLIENT_ID = os.environ["API_CLIENT_ID"]


async def main():
    token = DefaultAzureCredential().get_token(f"api://{API_CLIENT_ID}/.default").token
    async with httpx.AsyncClient(headers={"Authorization": f"Bearer {token}"}) as http_client:
        async with Client(streamable_http_client(SERVER_URL, http_client=http_client)) as client:
            tools = await client.list_tools()
            print("tools:", [t.name for t in tools.tools])
            found = await client.call_tool("search_employees", {"search_term": "baking"})
            print(found.content[0].text.splitlines()[1:4])


asyncio.run(main())
