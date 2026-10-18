import argparse
import os

from mcp.server.mcpserver import MCPServer

mcp = MCPServer("transport-lab")
TRANSPORT = "stdio"


@mcp.tool()
def server_info() -> dict:
    """Reports which process and transport answered the call."""
    return {"pid": os.getpid(), "transport": TRANSPORT}


@mcp.tool()
def add(a: int, b: int) -> int:
    """Adds two integers."""
    return a + b


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--transport", choices=["stdio", "http", "sse"], default="stdio")
    parser.add_argument("--port", type=int, default=47004)
    args = parser.parse_args()
    TRANSPORT = args.transport
    if args.transport == "stdio":
        mcp.run("stdio")
    elif args.transport == "http":
        mcp.run("streamable-http", host="127.0.0.1", port=args.port)
    else:
        mcp.run("sse", host="127.0.0.1", port=args.port)
