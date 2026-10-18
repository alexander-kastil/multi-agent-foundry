# Compare stdio and Streamable HTTP with the MCP Inspector

A server that works over stdio on your laptop fails when an agent needs to reach it remotely, and the cause is usually hidden inside the transport. You will run one small server over stdio and Streamable HTTP, read the raw messages of both protocol eras with `httpx`, and debug the same server with the MCP Inspector. A C# project repeats the comparison with the C# SDK.

## Prerequisites

- Python 3.12, .NET 10 SDK and Node.js 20 or later.
- Ports 47004 (Python HTTP), 47005 (legacy SSE) and 47006 (C# HTTP) free.
- No Azure resources are needed for this demo.

## Run it

```powershell
cd demos/02-mcp/02-transports/transports-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python server.py --transport http
```

The server prints nothing and listens on `http://127.0.0.1:47004/mcp`. Run the remaining commands in a second terminal with the same virtual environment active.

## Step 1: Run the server over stdio

`server.py` has two tools. `server_info` reports the process id and the transport that answered, which makes the difference between the transports visible.

```python
@mcp.tool()
def server_info() -> dict:
    """Reports which process and transport answered the call."""
    return {"pid": os.getpid(), "transport": TRANSPORT}
```

`stdio_client.py` does not connect to a running server. It starts `server.py --transport stdio` as a child process and talks to its standard streams.

```python
params = StdioServerParameters(command=sys.executable, args=["server.py", "--transport", "stdio"])
async with Client(params) as client:
    info = await client.call_tool("server_info", {})
```

```powershell
python stdio_client.py
python stdio_client.py
```

Expected result (the process id changes on every run because every run starts a new server):

```text
stdio -> {
  "pid": 145616,
  "transport": "stdio"
}
add   -> {'result': 5}
```

## Step 2: Connect to the same server over HTTP

`http_client.py` connects to the server you started in the first terminal. The process id stays the same across runs because the server outlives its clients.

```python
async with Client(URL) as client:
    info = await client.call_tool("server_info", {})
```

```powershell
python http_client.py
python http_client.py
```

Expected result:

```text
http  -> {
  "pid": 79028,
  "transport": "http"
}
add   -> {'result': 5}
```

That is the practical difference: stdio gives every client its own server process, and Streamable HTTP shares one server between clients.

![Every stdio_client.py run starts its own server process over stdin and stdout, while two http_client.py runs share one server on port 47004](../pptx/images/02-01-server-processes.svg)

## Step 3: Read the wire format of both protocol eras

`wire.py` sends raw JSON-RPC with `httpx`. First it speaks the 2025-11-25 era with an `initialize` handshake and a session id, then the 2026-07-28 era with no handshake.

![The 2025-11-25 era needs initialize and repeats the session id on every later request, while the 2026-07-28 era sends one self-contained tools/call and gets JSON back](../pptx/images/02-02-protocol-eras.svg)

```python
meta = {"io.modelcontextprotocol/protocolVersion": "2026-07-28", "io.modelcontextprotocol/clientCapabilities": {}}
modern_headers = {"MCP-Protocol-Version": "2026-07-28", "Mcp-Method": "tools/call", "Mcp-Name": "add"}
call = {"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "add", "arguments": {"a": 2, "b": 3}, "_meta": meta}}
show("2026-07-28 tools/call, no handshake, no session", post(call, modern_headers))
```

```powershell
python wire.py
```

Expected result (trimmed):

```text
== 2025-11-25 initialize
200 text/event-stream session: 5492e0b6ed3541e2a3c93f935749ac0d
event: message | data: {"jsonrpc":"2.0","id":1,"result":{"capabilities":{"prompts":{"listChanged":false},...,"protocolVersion":"2025-11-25","serverInfo":{"name":"transport-lab",...

== 2025-11-25 tools/call with the session id
200 text/event-stream session: 5492e0b6ed3541e2a3c93f935749ac0d
event: message | data: {"jsonrpc":"2.0","id":2,"result":{"content":[{"text":"5","type":"text"}],"isError":false,"structuredContent":{"result":5}}}

== 2026-07-28 tools/call, no handshake, no session
200 application/json session: None
{"jsonrpc":"2.0","id":3,"result":{"content":[{"text":"5","type":"text"}],"isError":false,"resultType":"complete","structuredContent":{"result":5},"_meta":{"io.modelcontextprotocol/serverInfo":{"name":"transport-lab","version":""}}}}
```

The older era answered as an SSE event and issued a session id. The new era answered a single JSON object and needed no id.

## Step 4: Debug with the MCP Inspector

The Inspector replaces `wire.py` for day-to-day work. The CLI mode calls one method and prints JSON. The `--protocol-era` option forces an era.

```powershell
npx @modelcontextprotocol/inspector --cli http://127.0.0.1:47004/mcp --transport http --protocol-era legacy --method tools/call --tool-name add --tool-arg a=2 --tool-arg b=3
npx @modelcontextprotocol/inspector --cli http://127.0.0.1:47004/mcp --transport http --protocol-era modern --method tools/call --tool-name add --tool-arg a=2 --tool-arg b=3
```

Expected result (the modern answer carries server identity in `_meta`):

```text
{"content":[{"type":"text","text":"5"}],"structuredContent":{"result":5},"isError":false}
{"_meta":{"io.modelcontextprotocol/serverInfo":{"name":"transport-lab","version":""}},"content":[{"type":"text","text":"5"}],"structuredContent":{"result":5},"isError":false}
```

The web mode gives the interactive view with a tool list, forms and a request history.

```powershell
npx @modelcontextprotocol/inspector --web --transport http --server-url http://127.0.0.1:47004/mcp
```

It prints `MCP Inspector Web is up and running at: http://127.0.0.1:6274?MCP_INSPECTOR_API_TOKEN=...`. Open that exact URL, which includes the session token. Stop it with Ctrl+C.

The deprecated SSE transport is still available in the SDK, which lets you see why it was replaced. Start it on its own port and open the stream.

```powershell
python server.py --transport sse --port 47005
curl.exe -N http://127.0.0.1:47005/sse
```

Expected result:

```text
event: endpoint
data: /messages/?session_id=9218174621594035bf6025659af33345
```

SSE needs a long-lived GET stream plus a second POST endpoint per client, which is the state that Streamable HTTP removed. Stop the SSE server with Ctrl+C before you continue.

## Step 5: Repeat the comparison in C#

`transports-cs` is one project with a server and a client. The same `LabTools` class backs both transports, and the client uses `StdioClientTransport` or `HttpClientTransport` from `ModelContextProtocol.Client`.

```csharp
IClientTransport transport = mode == "http"
    ? new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri("http://127.0.0.1:47006/mcp") })
    : new StdioClientTransport(new StdioClientTransportOptions
    {
        Command = "dotnet",
        Arguments = ["run", "--no-build", "--project", ".", "--", "--stdio"]
    });

await using var client = await McpClient.CreateAsync(transport);
```

Build once, start the HTTP server in a new terminal, and run both clients.

```powershell
cd demos/02-mcp/02-transports/transports-cs
dotnet build
dotnet run --no-build
```

```powershell
dotnet run --no-build -- --client http
dotnet run --no-build -- --client stdio
```

Expected result:

```text
http -> {"pid":141704,"transport":"http"}
add -> 5
stdio -> {"pid":30724,"transport":"stdio"}
add -> 5
```

## Verify

You are done when all of these hold:

- Two `stdio_client.py` runs print two different process ids, and two `http_client.py` runs print the same one.
- `wire.py` shows a session id for the 2025-11-25 call and `session: None` for the 2026-07-28 call.
- The Inspector returns `5` in both eras.
- The C# clients show the same split between the stdio and HTTP process ids.

## Cleanup

Stop the HTTP servers on ports 47004 and 47006 with Ctrl+C. Nothing was created in Azure.

```powershell
Get-NetTCPConnection -LocalPort 47004,47005,47006 -State Listen -ErrorAction SilentlyContinue
```

The command prints nothing once all three ports are free.
