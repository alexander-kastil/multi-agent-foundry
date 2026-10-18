import os

import uvicorn
from dotenv import load_dotenv
from mcp.server.auth.settings import AuthSettings
from mcp.server.mcpserver import MCPServer

load_dotenv()

from entra_verifier import API_CLIENT_ID, ISSUER, EntraTokenVerifier
from store import EmployeeStore

PORT = int(os.getenv("MCP_PORT", "47011"))
store = EmployeeStore()

mcp = MCPServer(
    "hr-mcp-server-auth-py",
    token_verifier=EntraTokenVerifier(),
    auth=AuthSettings(
        issuer_url=ISSUER,
        resource_server_url=f"http://localhost:{PORT}/mcp",
        required_scopes=["hr.read"],
        validate_token_resource=False,
    ),
)


@mcp.tool(name="list_employees")
def list_employees() -> list[dict]:
    """Lists every employee."""
    return [e.to_dict() for e in store.all()]


@mcp.tool(name="search_employees")
def search_employees(search_term: str) -> list[dict]:
    """Finds employees whose name, email, role or skills contain the search term."""
    return [e.to_dict() for e in store.search(search_term)]


if __name__ == "__main__":
    uvicorn.run(mcp.streamable_http_app(), host="127.0.0.1", port=PORT, log_level="warning")
