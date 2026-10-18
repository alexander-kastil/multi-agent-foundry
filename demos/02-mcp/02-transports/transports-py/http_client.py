import asyncio
import os

from dotenv import load_dotenv
from mcp import Client

load_dotenv()
URL = os.getenv("MCP_HTTP_URL", "http://127.0.0.1:47004/mcp")


async def main():
    async with Client(URL) as client:
        info = await client.call_tool("server_info", {})
        print("http  ->", info.content[0].text)
        total = await client.call_tool("add", {"a": 2, "b": 3})
        print("add   ->", total.structured_content)


asyncio.run(main())
