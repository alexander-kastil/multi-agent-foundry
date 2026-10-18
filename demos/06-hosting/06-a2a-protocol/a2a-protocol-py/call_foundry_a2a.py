import asyncio
import json
import os

import httpx
from a2a.client import A2ACardResolver, ClientConfig, create_client
from a2a.helpers import new_text_message
from a2a.types.a2a_pb2 import Role, SendMessageRequest
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv
from google.protobuf.json_format import MessageToDict

load_dotenv(override=True)

A2A_BASE_URL = f"{os.environ['PROJECT_ENDPOINT']}/agents/m6h2-a2a-agent/endpoint/protocols/a2a"


async def main() -> None:
    token = DefaultAzureCredential().get_token("https://ai.azure.com/.default").token
    async with httpx.AsyncClient(headers={"Authorization": f"Bearer {token}"}, timeout=httpx.Timeout(120.0)) as http_client:
        resolver = A2ACardResolver(httpx_client=http_client, base_url=A2A_BASE_URL, agent_card_path="agentCard/v1.0")
        card = await resolver.get_agent_card()
        print(f"card: {card.name} version={card.version} skills={[skill.id for skill in card.skills]}")

        client = await create_client(agent=card, client_config=ClientConfig(streaming=False, httpx_client=http_client))
        request = SendMessageRequest(message=new_text_message("What does the release desk cover?", role=Role.ROLE_USER))
        async for response in client.send_message(request):
            print(json.dumps(MessageToDict(response), indent=2)[:1200])
        await client.close()


asyncio.run(main())
