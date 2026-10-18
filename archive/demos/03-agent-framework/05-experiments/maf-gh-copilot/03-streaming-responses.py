"""
GitHub Copilot Agent streaming responses example.

This demo shows how to stream responses as they are generated
instead of waiting for the complete result.
"""

import asyncio

from agent_framework.github import GitHubCopilotAgent


async def main():
    agent = GitHubCopilotAgent(
        default_options={"instructions": "You are a helpful assistant."},
    )

    async with agent:
        print("Agent: ", end="", flush=True)
        async for chunk in agent.run_stream("Tell me a short story."):
            if chunk.text:
                print(chunk.text, end="", flush=True)
        print()


if __name__ == "__main__":
    asyncio.run(main())
