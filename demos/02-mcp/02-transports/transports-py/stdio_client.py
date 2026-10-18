import asyncio
import sys

from mcp import Client, StdioServerParameters


async def main():
    params = StdioServerParameters(command=sys.executable, args=["server.py", "--transport", "stdio"])
    async with Client(params) as client:
        info = await client.call_tool("server_info", {})
        print("stdio ->", info.content[0].text)
        total = await client.call_tool("add", {"a": 2, "b": 3})
        print("add   ->", total.structured_content)


asyncio.run(main())
