"""
GitHub Copilot Agent with MCP servers example.

This demo shows how to connect to local (stdio) and remote (HTTP) MCP servers,
giving the agent access to external tools and data sources.
"""

import asyncio

from agent_framework.github import GitHubCopilotAgent
from copilot.types import PermissionRequest, PermissionRequestResult, MCPServerConfig


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
    mcp_servers: dict[str, MCPServerConfig] = {
        # Local stdio server
        "filesystem": {
            "type": "stdio",
            "command": "npx",
            "args": ["-y", "@modelcontextprotocol/server-filesystem", "."],
            "tools": ["*"],
        },
        # Remote HTTP server
        "microsoft-learn": {
            "type": "http",
            "url": "https://learn.microsoft.com/api/mcp",
            "tools": ["*"],
        },
    }

    agent = GitHubCopilotAgent(
        default_options={
            "instructions": "You are a helpful assistant with access to the filesystem and Microsoft Learn.",
            "on_permission_request": prompt_permission,
            "mcp_servers": mcp_servers,
        },
    )

    async with agent:
        result = await agent.run("Search Microsoft Learn for 'Azure Functions' and summarize the top result")
        print(result)


if __name__ == "__main__":
    asyncio.run(main())
