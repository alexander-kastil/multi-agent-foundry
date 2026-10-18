import asyncio
import json
import sys
from pathlib import Path

from agent_framework import AgentSession

from agent_factory import build_agent

sys.stdout.reconfigure(encoding="utf-8")

REGISTRY_FILE = Path("sessions/registry.json")


def load_registry() -> dict[str, dict]:
    return json.loads(REGISTRY_FILE.read_text()) if REGISTRY_FILE.exists() else {}


async def main(user: str, message: str) -> None:
    registry = load_registry()
    agent = build_agent()

    if user in registry:
        session = AgentSession.from_dict(registry[user])
    else:
        session = agent.create_session()

    result = await agent.run(message, session=session)
    print(f"{user}: {result.text}")

    registry[user] = session.to_dict()
    REGISTRY_FILE.parent.mkdir(exist_ok=True)
    REGISTRY_FILE.write_text(json.dumps(registry, indent=2))


asyncio.run(main(sys.argv[1], sys.argv[2]))
