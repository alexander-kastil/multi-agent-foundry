"""
Basic GitHub Copilot Agent example.

This demo shows how to create a simple agent that answers questions
using GitHub Copilot through the Microsoft Agent Framework.
"""

import asyncio

from agent_framework.github import GitHubCopilotAgent


async def main():
    agent = GitHubCopilotAgent(
        default_options={"instructions": "You are a helpful assistant."},
    )

    async with agent:
        result = await agent.run("What is Microsoft Agent Framework?")
        print(f"Agent: {result}")


if __name__ == "__main__":
    asyncio.run(main())
