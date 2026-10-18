"""
GitHub Copilot Agent multi-turn conversations example.

This demo shows how to maintain conversation context across multiple
interactions using threads.
"""

import asyncio

from agent_framework.github import GitHubCopilotAgent


async def main():
    agent = GitHubCopilotAgent(
        default_options={"instructions": "You are a helpful assistant."},
    )

    async with agent:
        thread = agent.get_new_thread()

        # First interaction
        result1 = await agent.run("My name is Alice.", thread=thread)
        print(f"Agent: {result1}")

        # Second interaction - agent remembers the context
        result2 = await agent.run("What's my name?", thread=thread)
        print(f"Agent: {result2}")  # Should remember "Alice"


if __name__ == "__main__":
    asyncio.run(main())
