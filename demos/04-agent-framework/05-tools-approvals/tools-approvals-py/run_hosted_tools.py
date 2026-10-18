import asyncio
import sys

from agent_framework.foundry import FoundryChatClient

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    code_interpreter = FoundryChatClient.get_code_interpreter_tool()
    learn = FoundryChatClient.get_mcp_tool(
        name="microsoft-learn",
        url="https://learn.microsoft.com/api/mcp",
        approval_mode="never_require",
    )

    agent = build_agent([code_interpreter, learn])

    result = await agent.run("Use code to compute the total price of 3 nights for 4 hikers at 38 euro per person and night, and give the number.")
    print(result.text)

    result = await agent.run("Search Microsoft Learn: which package contains FoundryChatClient?")
    print(result.text)


asyncio.run(main())
