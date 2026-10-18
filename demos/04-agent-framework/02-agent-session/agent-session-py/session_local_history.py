import asyncio
import json
import sys
from pathlib import Path

from agent_framework import AgentSession

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")

SESSION_FILE = Path("sessions/local-session.json")
OPTIONS = {"store": False}


async def main(message: str) -> None:
    agent = build_agent()

    if SESSION_FILE.exists():
        session = AgentSession.from_dict(json.loads(SESSION_FILE.read_text()))
    else:
        session = agent.create_session()

    result = await agent.run(message, session=session, options=OPTIONS)
    print(result.text)

    data = session.to_dict()
    print(f"service_session_id: {data['service_session_id']}")
    print(f"stored messages   : {len(data['state']['in_memory']['messages'])}")

    SESSION_FILE.parent.mkdir(exist_ok=True)
    SESSION_FILE.write_text(json.dumps(data, indent=2))


asyncio.run(main(sys.argv[1]))
