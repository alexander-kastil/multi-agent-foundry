import asyncio
import sys

from agent_framework import Message

from agent_factory import build_agent
from trail_tools import book_hut, estimate_price

sys.stdout.reconfigure(encoding="utf-8")

PROMPT = "Book the Erzherzog Johann Hut for 2 hikers for 1 night."


async def main(decision: bool) -> None:
    agent = build_agent([estimate_price, book_hut])
    session = agent.create_session()

    result = await agent.run(PROMPT, session=session)
    while result.user_input_requests:
        responses = []
        for request in result.user_input_requests:
            call = request.function_call
            print(f"approval requested: {call.name}({call.arguments})")
            print(f"decision: {'approve' if decision else 'reject'}")
            responses.append(Message(role="user", contents=[request.to_function_approval_response(decision)]))
        result = await agent.run(responses, session=session)

    print(result.text)


asyncio.run(main(sys.argv[1] == "approve"))
