import json
import os

import httpx
from dotenv import load_dotenv

load_dotenv()
URL = os.getenv("MCP_HTTP_URL", "http://127.0.0.1:47004/mcp")
BASE = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}


def show(title, response):
    print(f"== {title}")
    print(response.status_code, response.headers.get("content-type"), "session:", response.headers.get("mcp-session-id"))
    print(response.text.strip().replace("\n", " | ")[:260])
    print()


def post(body, headers=None):
    return httpx.post(URL, headers={**BASE, **(headers or {})}, content=json.dumps(body))


legacy = post({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2025-11-25", "capabilities": {}, "clientInfo": {"name": "wire", "version": "1"}}})
show("2025-11-25 initialize", legacy)

session = legacy.headers["mcp-session-id"]
headers = {"Mcp-Session-Id": session, "MCP-Protocol-Version": "2025-11-25"}
post({"jsonrpc": "2.0", "method": "notifications/initialized"}, headers)
call = {"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": "add", "arguments": {"a": 2, "b": 3}}}
show("2025-11-25 tools/call with the session id", post(call, headers))

meta = {"io.modelcontextprotocol/protocolVersion": "2026-07-28", "io.modelcontextprotocol/clientCapabilities": {}}
modern_headers = {"MCP-Protocol-Version": "2026-07-28", "Mcp-Method": "tools/call", "Mcp-Name": "add"}
call = {"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "add", "arguments": {"a": 2, "b": 3}, "_meta": meta}}
show("2026-07-28 tools/call, no handshake, no session", post(call, modern_headers))
