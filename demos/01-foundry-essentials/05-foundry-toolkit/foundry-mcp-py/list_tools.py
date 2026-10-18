import asyncio
from collections import Counter
from foundry_mcp import connect


async def main():
    async with connect() as client:
        tools = (await client.list_tools()).tools

    print(f"tools exposed: {len(tools)}")
    groups = Counter(tool.name.split("_")[0] for tool in tools)
    for group, count in sorted(groups.items()):
        print(f"{group:<12} {count}")

    print()
    for tool in tools:
        if tool.name in ("model_deployment_get", "agent_update"):
            print(tool.name)
            for name, schema in tool.input_schema["properties"].items():
                print(f"  {name:<28} {schema.get('type', 'object')}")


asyncio.run(main())
