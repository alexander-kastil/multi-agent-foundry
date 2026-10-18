import asyncio
import sys

from agent_framework import Message, create_always_approve_tool_response, get_agent_mode, set_agent_mode

from agent_factory import build_harness
from trail_tools import RESERVATIONS

sys.stdout.reconfigure(encoding="utf-8")

PLAN_REQUEST = (
    "Plan a 4 stage Grossglockner trek for 3 hikers, including a hut reservation for every open stage. The stages are: "
    "Stage 1 Heiligenblut to Elberfelder Hut, Stage 2 Elberfelder Hut to Salm Hut, "
    "Stage 3 Salm Hut to Glocknerhaus, Stage 4 Glocknerhaus to Kals."
)


def show_todos(session) -> None:
    for item in session.state["todo"]["items"]:
        mark = "x" if item["is_complete"] else " "
        print(f"  [{mark}] {item['id']}. {item['title']}")


async def main() -> None:
    agent = build_harness()
    session = agent.create_session()

    result = await agent.run(PLAN_REQUEST, session=session)
    print(result.messages[-1].text)
    print(f"\nmode: {get_agent_mode(session)}")
    show_todos(session)

    set_agent_mode(session, "execute")
    print(f"\nmode: {get_agent_mode(session)}")

    result = await agent.run("The plan is approved. Execute it, including the hut reservations.", session=session)
    while result.user_input_requests:
        responses = []
        for request in result.user_input_requests:
            call = request.function_call
            print(f"approval requested: {call.name}({call.arguments})")
            responses.append(Message(role="user", contents=[create_always_approve_tool_response(request)]))
        result = await agent.run(responses, session=session)

    print(result.messages[-1].text)
    print()
    show_todos(session)
    print(f"reservations executed: {RESERVATIONS}")


asyncio.run(main())
