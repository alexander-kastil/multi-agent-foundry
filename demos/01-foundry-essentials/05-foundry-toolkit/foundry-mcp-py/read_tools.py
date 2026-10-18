import asyncio
import json
import os
import sys
from dotenv import load_dotenv
from foundry_mcp import connect

load_dotenv(override=True)
sys.stdout.reconfigure(encoding="utf-8")

DEPLOYMENT = sys.argv[1] if len(sys.argv) > 1 else "m1-router-subset"


async def main():
    async with connect() as client:
        catalog = await client.call_tool("model_catalog_list", {"modelName": "model-router"})
        entry = json.loads(catalog.content[0].text)[0]
        print(f"catalog   : {entry['name']} by {entry['publisher']}")
        print(f"asset     : {entry['assetId']}")

        result = await client.call_tool(
            "model_deployment_get",
            {
                "foundryAccountResourceId": os.environ["FOUNDRY_ACCOUNT_RESOURCE_ID"],
                "deploymentName": DEPLOYMENT,
            },
        )
        deployment = json.loads(result.content[0].text)[0]
        properties = deployment["data"]["properties"]
        print()
        print(f"deployment: {deployment['name']} ({deployment['kind']}, {deployment['provisioningState']})")
        print(f"model     : {properties['model']['name']} {properties['model']['version']}")
        print(f"capacity  : {deployment['data']['sku']['name']} x {properties['currentCapacity']}")
        for model in properties["routing"]["models"]:
            print(f"routes to : {model['name']} {model['version']}")


asyncio.run(main())
