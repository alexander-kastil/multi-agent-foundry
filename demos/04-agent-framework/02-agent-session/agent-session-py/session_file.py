import asyncio
import json
import sys
from pathlib import Path

from agent_framework import AgentSession

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")

SESSION_FILE = Path("sessions/session.json")


async def main(message: str) -> None:
    agent = build_agent()

    if SESSION_FILE.exists():
        session = AgentSession.from_dict(json.loads(SESSION_FILE.read_text()))
        print(f"resumed session {session.session_id}")
    else:
        session = agent.create_session()
        print(f"new session {session.session_id}")

    result = await agent.run(message, session=session)
    print(result.text)

    SESSION_FILE.parent.mkdir(exist_ok=True)
    SESSION_FILE.write_text(json.dumps(session.to_dict(), indent=2))


asyncio.run(main(sys.argv[1]))
