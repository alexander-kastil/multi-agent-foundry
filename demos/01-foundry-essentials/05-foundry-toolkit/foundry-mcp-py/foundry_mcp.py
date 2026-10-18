import httpx2
from azure.identity import DefaultAzureCredential
from mcp import Client
from mcp.client.streamable_http import streamable_http_client

MCP_URL = "https://mcp.ai.azure.com"


def connect():
    token = DefaultAzureCredential().get_token(f"{MCP_URL}/.default").token
    http = httpx2.AsyncClient(headers={"Authorization": f"Bearer {token}"}, timeout=120)
    return Client(streamable_http_client(MCP_URL, http_client=http))
