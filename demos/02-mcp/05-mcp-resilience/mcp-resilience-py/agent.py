import asyncio
import os
import sys
import time

from agent_framework import Agent, MCPStreamableHTTPTool
from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential

from support import BASE, HEADERS, admin, section, show, start_server

sys.stdout.reconfigure(encoding="utf-8")

INSTRUCTIONS = (
    "You are a clinical decision support assistant for Northwind Health. "
    "Use the lookup_drug_interaction tool for every interaction question. "
    "If the tool reports status unavailable, say the check could not be completed, "
    "never state that no interaction exists, and follow the fallback it names. "
    "Keep answers under 60 words."
)


async def main():
    server = start_server()
    try:
        client = FoundryChatClient(
            project_endpoint=os.getenv("PROJECT_ENDPOINT"),
            model=os.getenv("MODEL_DEPLOYMENT_NAME"),
            credential=DefaultAzureCredential(exclude_environment_credential=True, exclude_managed_identity_credential=True),
        )
        mcp_tool = MCPStreamableHTTPTool(name="drug-tools", url=f"{BASE}/mcp", static_headers=HEADERS, approval_mode="never_require")

        async with Agent(client=client, instructions=INSTRUCTIONS, tools=[mcp_tool]) as agent:
            section("Healthy server")
            print((await agent.run("Is it safe to combine warfarin and aspirin?")).text)

            section("Permanent error")
            print((await agent.run("Check the interaction between zorbiflex and aspirin.")).text)

            section("Outage: the agent follows the fallback")
            admin("/admin/outage?on=1")
            started = time.time()
            print((await agent.run("Is it safe to combine atorvastatin and clarithromycin?")).text)
            print(f"first failing question took {time.time() - started:.1f}s")
            show("after the outage question")

            section("Recovery")
            admin("/admin/outage?on=0")
            time.sleep(10.5)
            print((await agent.run("Is it safe to combine atorvastatin and clarithromycin?")).text)
            show("after recovery")
    finally:
        server.terminate()


try:
    asyncio.run(main())
except KeyboardInterrupt:
    print("\nInterrupted.")
