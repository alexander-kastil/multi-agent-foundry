import asyncio
import sys

from agent_framework import MCPStreamableHTTPTool

from agent_factory import build_agent, tool_calls

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    learn = MCPStreamableHTTPTool(
        name="microsoft-learn",
        url="https://learn.microsoft.com/api/mcp",
    )

    async with build_agent(learn, instructions="Search Microsoft Learn before you answer. Answer in two sentences without dashes.") as agent:
        result = await agent.run("What does the approval_mode parameter of the Agent Framework tool decorator do?")
        for call in tool_calls(result):
            print(f"tool call: {call}")
        print(result.text)


asyncio.run(main())
