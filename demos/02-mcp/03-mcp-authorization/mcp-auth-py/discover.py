import os
import re

import httpx
from dotenv import load_dotenv

load_dotenv()
SERVER_URL = os.getenv("MCP_SERVER_URL", "http://localhost:47011/mcp")

challenge = httpx.post(
    SERVER_URL,
    headers={"Content-Type": "application/json", "Accept": "application/json, text/event-stream"},
    json={"jsonrpc": "2.0", "id": 1, "method": "tools/list"},
)
print("1. unauthenticated request ->", challenge.status_code)
header = challenge.headers["www-authenticate"]
print("   WWW-Authenticate:", header)

prm_url = re.search(r'resource_metadata="([^"]+)"', header).group(1)
prm = httpx.get(prm_url).json()
print("2. protected resource metadata ->", prm_url)
print("   resource:", prm["resource"])
print("   authorization_servers:", prm["authorization_servers"])

issuer = prm["authorization_servers"][0]
metadata = httpx.get(f"{issuer}/.well-known/openid-configuration").json()
print("3. authorization server metadata")
for key in ("issuer", "authorization_endpoint", "token_endpoint", "code_challenge_methods_supported"):
    print(f"   {key}:", metadata.get(key))
print("   registration_endpoint:", metadata.get("registration_endpoint", "not published (no dynamic client registration)"))
print("   client_id_metadata_document_supported:", metadata.get("client_id_metadata_document_supported", "not published"))
