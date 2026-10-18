import asyncio
import sys
from pathlib import Path

from agent_framework import MCPStdioTool, Message

from agent_factory import build_agent, tool_calls

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    trail_data = MCPStdioTool(
        name="trail-data",
        command=sys.executable,
        args=[str(Path(__file__).with_name("trail_mcp_server.py"))],
        approval_mode={
            "never_require_approval": ["get_forecast", "list_huts"],
            "always_require_approval": ["reserve_bed"],
        },
    )

    async with build_agent(trail_data) as agent:
        session = agent.create_session()
        result = await agent.run(
            "Which huts are in Tyrol, what is the forecast there, and reserve 2 beds at Similaun Hut?",
            session=session,
        )
        for call in tool_calls(result):
            print(f"tool call: {call}")

        while result.user_input_requests:
            responses = []
            for request in result.user_input_requests:
                print(f"approval requested: {request.function_call.name}({request.function_call.arguments})")
                responses.append(Message(role="user", contents=[request.to_function_approval_response(True)]))
            result = await agent.run(responses, session=session)

        print(result.text)


asyncio.run(main())
