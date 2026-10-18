"""
GitHub Copilot Agent with permissions example.

This demo shows how to enable the agent to execute shell commands,
read/write files, and fetch URLs with permission prompts.
"""

import asyncio

from agent_framework.github import GitHubCopilotAgent
from copilot.types import PermissionRequest, PermissionRequestResult


def prompt_permission(
    request: PermissionRequest, context: dict[str, str]
) -> PermissionRequestResult:
    kind = request.get("kind", "unknown")
    print(f"\n[Permission Request: {kind}]")

    response = input("Approve? (y/n): ").strip().lower()
    if response in ("y", "yes"):
        return PermissionRequestResult(kind="approved")
    return PermissionRequestResult(kind="denied-interactively-by-user")


async def main():
    agent = GitHubCopilotAgent(
        default_options={
            "instructions": "You are a helpful assistant that can execute shell commands.",
            "on_permission_request": prompt_permission,
        },
    )

    async with agent:
        result = await agent.run("List the Python files in the current directory")
        print(result)


if __name__ == "__main__":
    asyncio.run(main())
