import asyncio
import json
import sys
from pathlib import Path

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    saved = json.loads(Path("sessions/session.json").read_text())
    service_session_id = saved["service_session_id"]
    print(f"service_session_id: {service_session_id}")

    agent = build_agent()
    session = agent.get_session(service_session_id=service_session_id)
    result = await agent.run("Summarize what you know about my trip in one sentence.", session=session)
    print(result.text)


asyncio.run(main())
