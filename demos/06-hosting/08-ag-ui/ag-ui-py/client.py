import asyncio
import os
import sys

from agent_framework import Agent
from agent_framework_ag_ui import AGUIChatClient
from dotenv import load_dotenv

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")


async def main() -> None:
    endpoint = f"http://127.0.0.1:{os.getenv('AGUI_PORT', '8888')}/"
    agent = Agent(name="remote-desk", client=AGUIChatClient(endpoint=endpoint))
    session = agent.create_session()

    for question in ("What is the status of T-1002?", "Which team owns it?"):
        print(f"user: {question}")
        print("agent: ", end="")
        async for update in agent.run(question, session=session, stream=True):
            if update.text:
                print(update.text, end="", flush=True)
        print()


asyncio.run(main())
