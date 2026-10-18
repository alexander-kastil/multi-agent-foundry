import asyncio
import json
import os
import sys
from dotenv import load_dotenv
from foundry_mcp import connect

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

AGENT = "m1-mcp-agent"
ENDPOINT = os.environ["PROJECT_ENDPOINT"]
DEFINITION = {
    "kind": "prompt",
    "model": os.environ["MODEL_DEPLOYMENT_NAME"],
    "instructions": "You explain Microsoft Foundry concepts in one short sentence and always call the product Microsoft Foundry.",
}


async def main():
    async with connect() as client:
        created = await client.call_tool(
            "agent_update",
            {"projectEndpoint": ENDPOINT, "agentName": AGENT, "agentDefinition": DEFINITION},
        )
        agent = json.loads(created.content[0].text)
        version = agent["versions"]["latest"]
        print(f"created   : {agent['id']} version {version['version']} ({version['definition']['kind']})")

        reply = await client.call_tool(
            "agent_invoke",
            {"projectEndpoint": ENDPOINT, "agentName": AGENT, "inputText": "What is a Foundry project?"},
        )
        response = json.loads(reply.content[0].text)
        print(f"status    : {response['status']}")
        print(f"answer    : {response['output'][0]['content'][0]['text']}")

        deleted = await client.call_tool("agent_delete", {"projectEndpoint": ENDPOINT, "agentName": AGENT})
        print(f"deleted   : {json.loads(deleted.content[0].text)['deleted']}")

        lookup = await client.call_tool("agent_get", {"projectEndpoint": ENDPOINT, "agentName": AGENT})
        print(f"lookup    : {'not found' if 'not_found' in lookup.content[0].text else 'still present'}")


asyncio.run(main())
