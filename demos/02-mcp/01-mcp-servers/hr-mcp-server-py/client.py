import asyncio
import sys

from mcp import Client, StdioServerParameters


async def main(target):
    async with Client(target) as client:
        tools = await client.list_tools()
        print("tools:", [t.name for t in tools.tools])
        resources = await client.list_resources()
        print("resources:", [str(r.uri) for r in resources.resources])
        templates = await client.list_resource_templates()
        print("templates:", [t.uri_template for t in templates.resource_templates])
        prompts = await client.list_prompts()
        print("prompts:", [p.name for p in prompts.prompts])

        found = await client.call_tool("search_employees", {"search_term": "baking"})
        print("search_employees:", found.content[0].text)

        policy = await client.read_resource("hr://policy/remote-work")
        print("policy:", policy.contents[0].text.splitlines()[0])

        profile = await client.read_resource("hr://employees/luca.rossi@restaurant.com")
        print("profile:", profile.contents[0].text)

        prompt = await client.get_prompt("onboarding_plan", {"role": "Line Cook", "buddy": "Carlos"})
        print("prompt:", prompt.messages[0].content.text)


if __name__ == "__main__":
    target = sys.argv[1] if len(sys.argv) > 1 else StdioServerParameters(command=sys.executable, args=["server.py"])
    asyncio.run(main(target))
