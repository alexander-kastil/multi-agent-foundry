import os

from agent_framework import Agent, tool
from agent_framework.foundry import FoundryChatClient
from agent_framework_foundry_hosting import ResponsesHostServer
from azure.identity import DefaultAzureCredential

RELEASES = {
    "portal": "2.4.0, shipped 2026-10-01, adds SSO and a new audit log",
    "gateway": "1.9.2, shipped 2026-09-24, fixes a token refresh bug",
    "mobile": "3.1.0, shipped 2026-10-12, adds offline mode",
}


@tool
def latest_release(product: str) -> str:
    """Return the latest release line for portal, gateway or mobile."""
    return RELEASES.get(product.lower(), f"No release data for {product}")


client = FoundryChatClient(
    project_endpoint=os.environ["FOUNDRY_PROJECT_ENDPOINT"],
    model=os.getenv("FOUNDRY_MODEL") or os.environ["AZURE_AI_MODEL_DEPLOYMENT_NAME"],
    credential=DefaultAzureCredential(),
)

agent = Agent(
    client=client,
    name="m6-azd-agent",
    instructions="You are the Contoso release notes agent. Use latest_release and answer in one sentence.",
    tools=[latest_release],
)

if __name__ == "__main__":
    ResponsesHostServer(agent).run()
