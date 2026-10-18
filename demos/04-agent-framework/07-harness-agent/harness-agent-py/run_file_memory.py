import asyncio
import sys
from pathlib import Path

from agent_framework import AgentSession, FileSystemAgentFileStore

from agent_factory import build_harness

sys.stdout.reconfigure(encoding="utf-8")

MEMORY_ROOT = Path("agent-file-memory")
SESSION_ID = "grossglockner-group"


def build():
    return build_harness(
        disable_file_memory=False,
        file_memory_store=FileSystemAgentFileStore(MEMORY_ROOT),
        disable_todo=True,
        disable_mode=True,
    )


async def main() -> None:
    first = build()
    result = await first.run(
        "Remember for later: our group of 3 uses trekking poles and avoids exposed ridges.",
        session=AgentSession(session_id=SESSION_ID),
    )
    print(result.messages[-1].text)

    for path in sorted(MEMORY_ROOT.rglob("*")):
        if path.is_file():
            print(f"file: {path.as_posix()}")
            print(path.read_text(encoding="utf-8").strip())

    second = build()
    result = await second.run(
        "Which stage should we skip, given what you know about our group?",
        session=AgentSession(session_id=SESSION_ID),
    )
    print(result.messages[-1].text)


asyncio.run(main())
