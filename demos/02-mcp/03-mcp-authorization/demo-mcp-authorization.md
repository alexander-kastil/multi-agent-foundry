# Secure an MCP Server with Microsoft Entra ID

An MCP server without authentication hands its tools to anyone who finds the URL. You will register the server as an API in Microsoft Entra ID, protect an ASP.NET Core MCP server with it, watch the discovery challenge and a scope-based step-up, and then protect a Python server the same way.

## Prerequisites

- `az login` as a user who can register applications in the tenant (at least the Application Developer role).
- .NET 10 SDK, Python 3.12.
- Ports 47010 (ASP.NET Core) and 47011 (Python) free.
- The demo creates one app registration named `m2-mcp-hr-api`. The cleanup step deletes it.

## Run it

```bash
cd demos/02-mcp/03-mcp-authorization
bash provision-entra.azcli
```

The script prints the two values the servers need. Export them for the ASP.NET Core server and write them into `mcp-auth-py/.env` for the Python server.

```text
TENANT_ID=<tenant-id>
API_CLIENT_ID=<api-client-id>
```

```bash
export Entra__TenantId=<tenant-id>
export Entra__ClientId=<api-client-id>
cd mcp-auth-cs
dotnet run --urls http://localhost:47010
```

## Step 1: Register the API in Entra

`provision-entra.azcli` creates the app registration with four settings that the MCP flow depends on.

```json
{
  "identifierUris": ["api://<api-client-id>"],
  "api": {
    "requestedAccessTokenVersion": 2,
    "oauth2PermissionScopes": [
      {"value": "hr.read", "type": "User", "isEnabled": true},
      {"value": "hr.write", "type": "User", "isEnabled": true}
    ]
  }
}
```

It also pre-authorizes the Azure CLI (client ID `04b07795-8ddb-461a-bbee-02f9e1bf7b46`) for `hr.read` only, and it creates the service principal. Pre-authorization lets the Azure CLI request the scope without a consent screen, which stands in for an interactive MCP client in this demo. Version 2 tokens make `aud` the API's client ID and `iss` the v2.0 issuer.

## Step 2: Watch the server challenge an unauthenticated client

The server calls `AddMcp` to publish metadata and to answer `401` with a pointer to it. Send a request without a token.

```bash
curl -i -X POST http://localhost:47010/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

Expected result (trimmed):

```text
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="http://localhost:47010/.well-known/oauth-protected-resource/mcp"
```

Follow the pointer to read the Protected Resource Metadata.

```bash
curl http://localhost:47010/.well-known/oauth-protected-resource/mcp
```

Expected result:

```text
{"resource":"http://localhost:47010/mcp","authorization_servers":["https://login.microsoftonline.com/<tenant-id>/v2.0"],"bearer_methods_supported":["header"],"scopes_supported":["api://<api-client-id>/hr.read","api://<api-client-id>/hr.write"]}
```

A client now knows which authorization server to use and which scopes the server offers.

## Step 3: Call the server with a token

Request a token for the API. The `.default` scope returns every scope the Azure CLI is pre-authorized for, which is `hr.read` at this point.

```bash
READ=$(az account get-access-token --scope api://<api-client-id>/.default --query accessToken -o tsv)
echo $READ | cut -d. -f2 | python -c "import sys,base64,json;s=sys.stdin.read().strip();s+='='*(-len(s)%4);d=json.loads(base64.urlsafe_b64decode(s));print({k:d[k] for k in ('aud','iss','scp','ver','azp') if k in d})"
```

Expected result:

```text
{'aud': '<api-client-id>', 'iss': 'https://login.microsoftonline.com/<tenant-id>/v2.0', 'scp': 'hr.read', 'ver': '2.0', 'azp': '04b07795-8ddb-461a-bbee-02f9e1bf7b46'}
```

Send the token to a read tool, and then send a token that was issued for a different API, here Azure Resource Manager.

```bash
curl -i -X POST http://localhost:47010/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "Authorization: Bearer $READ" -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"list_employees","arguments":{}}}'
ARM=$(az account get-access-token --resource https://management.azure.com --query accessToken -o tsv)
curl -i -X POST http://localhost:47010/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "Authorization: Bearer $ARM" -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

Expected result (trimmed):

```text
HTTP/1.1 200 OK
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="http://localhost:47010/.well-known/oauth-protected-resource/mcp"
```

The second token is a valid Entra token, but its audience is not this server, so the JWT bearer handler rejects it. This is the check that stops token passthrough between services.

## Step 4: Trigger and complete a step-up

The write tools need `hr.write`. The token from step 3 does not have it, so the middleware in `Auth/ScopeChallengeMiddleware.cs` answers `403` and names the scope.

```csharp
if (tool is not null && WriteTools.Contains(tool) && !HasScope(context.User, options.Value.WriteScope))
{
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    context.Response.Headers.WWWAuthenticate =
        $"Bearer error=\"insufficient_scope\", scope=\"api://{options.Value.ClientId}/{options.Value.WriteScope}\", resource_metadata=\"{metadata}\", error_description=\"The {tool} tool needs the {options.Value.WriteScope} scope\"";
    return;
}
```

```bash
curl -i -X POST http://localhost:47010/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "Authorization: Bearer $READ" -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"add_employee","arguments":{"firstName":"Ida","lastName":"Berger","email":"ida.berger@restaurant.com","currentRole":"Line Cook"}}}'
```

Expected result:

```text
HTTP/1.1 403 Forbidden
WWW-Authenticate: Bearer error="insufficient_scope", scope="api://<api-client-id>/hr.write", resource_metadata="http://localhost:47010/.well-known/oauth-protected-resource/mcp", error_description="The add_employee tool needs the hr.write scope"
```

An interactive client would now re-authorize with the union of its scopes and `hr.write`, which shows a consent screen. In this demo you grant the scope to the Azure CLI client in advance and request a token for it.

```bash
bash grant-write.azcli
WRITE=$(az account get-access-token --scope api://<api-client-id>/hr.write --query accessToken -o tsv)
curl -i -X POST http://localhost:47010/mcp -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -H "Authorization: Bearer $WRITE" -d '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"add_employee","arguments":{"firstName":"Ida","lastName":"Berger","email":"ida.berger@restaurant.com","currentRole":"Line Cook"}}}'
```

Expected result:

```text
HTTP/1.1 200 OK
data: {"result":{"content":[{"type":"text","text":"Added Ida Berger"}],...
```

Pre-authorization takes a few seconds to propagate. If the first request still returns `403`, wait ten seconds and request a fresh token.

## Step 5: Protect a Python server the same way

`mcp-auth-py/server.py` gives the Python SDK a `TokenVerifier` and `AuthSettings`. The SDK serves the metadata route and the `401` challenge, and `entra_verifier.py` validates the token with `PyJWT` against the tenant's signing keys.

```python
mcp = MCPServer(
    "hr-mcp-server-auth-py",
    token_verifier=EntraTokenVerifier(),
    auth=AuthSettings(issuer_url=ISSUER, resource_server_url=f"http://localhost:{PORT}/mcp", required_scopes=["hr.read"], validate_token_resource=False),
)
```

Start it in a new terminal with the two IDs in `.env`, then run the discovery script, which performs the client side of steps 1 to 3 of the specification flow.

```powershell
cd demos/02-mcp/03-mcp-authorization/mcp-auth-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
copy .env.example .env
python server.py
```

```powershell
python discover.py
python call_with_token.py
```

Expected result (trimmed, with `<tenant-id>` standing for your tenant):

```text
1. unauthenticated request -> 401
   WWW-Authenticate: Bearer error="invalid_token", error_description="Authentication required", resource_metadata="http://localhost:47011/.well-known/oauth-protected-resource/mcp"
2. protected resource metadata -> http://localhost:47011/.well-known/oauth-protected-resource/mcp
   resource: http://localhost:47011/mcp
   authorization_servers: ['https://login.microsoftonline.com/<tenant-id>/v2.0']
3. authorization server metadata
   issuer: https://login.microsoftonline.com/<tenant-id>/v2.0
   authorization_endpoint: https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/authorize
   token_endpoint: https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token
   code_challenge_methods_supported: None
   registration_endpoint: not published (no dynamic client registration)
   client_id_metadata_document_supported: not published
tools: ['list_employees', 'search_employees']
['  "first_name": "Marie",', '  "last_name": "Dubois",', '  "email": "marie.dubois@restaurant.com",']
```

`discover.py` shows what Entra does not publish. There is no registration endpoint and no Client ID Metadata Document flag, so a real MCP client needs a pre-registered client ID for Entra. `call_with_token.py` uses `DefaultAzureCredential` to get the token and an `httpx` client to attach it.

## Step 6: Connect a Foundry agent (reference only)

Agent Service reaches only remote endpoints, so this step is not part of the local run. Once the server runs on a public HTTPS URL, such as an Azure Container Apps endpoint, you create a project connection that carries the identity. For Microsoft Entra authentication with the agent identity, the connection needs the server's audience.

```bash
azd ai project set "https://<resource>.services.ai.azure.com/api/projects/<project>"
azd ai connection create hr-mcp \
  --kind remote-tool \
  --target https://<your-public-server>/mcp \
  --auth-type agentic-identity \
  --audience "api://<api-client-id>"
```

The agent then lists the connection in the `project_connection_id` field of its `mcp` tool. For per-user access you create the connection with OAuth identity passthrough instead, and your application surfaces the `consent_link` from the `oauth_consent_request` item. The command above is taken from Microsoft Learn and was not run in this course, because it needs a public endpoint.

## Verify

You are done when all of these hold:

- A request without a token returns `401` with a `resource_metadata` pointer on both servers.
- A token for another audience returns `401`, and the read token lists employees.
- The read token receives `403` with `insufficient_scope` for `add_employee`, and the `hr.write` token adds an employee.
- `call_with_token.py` lists the tools of the Python server.

## Cleanup

Stop both servers with Ctrl+C and delete the app registration.

```bash
az ad app delete --id <api-client-id>
```
