# Entra Agent ID: On-Behalf-Of Flows

An agent that calls tools needs an identity that is not a person and not a shared secret. Microsoft Foundry gives every agent its own Microsoft Entra agent identity, and it decides per tool connection whether a call runs as the agent, as the project, or on behalf of the signed-in user. This topic shows what those identities are, how to grant them access, and how OAuth identity passthrough carries a user's permissions to an MCP server.

## Every agent has its own identity

An agent identity is a special service principal in Microsoft Entra ID that represents the agent at runtime. It is created from an agent identity blueprint, which is the governing template for a class of agents: administrators apply Conditional Access to it, revoke permissions for every agent of that kind, and audit them together. In the agent object model, a newly created agent receives its own blueprint and identity, and the identity does not change when you add versions.

```python
agent = project.agents.get(agent_name="m8-idn-orders")
print(agent.instance_identity["principal_id"])
print(agent.blueprint_reference["blueprint_id"])
```

Entra shows the identity as a service principal of type `ServiceIdentity` with a display name that ends in `-AgentIdentity`. Because it is a separate principal per agent, an orders agent and an HR agent hold different access, and the audit log tells them apart.

## Project managed identity and agent identity

A Foundry project also has a system-assigned managed identity. The two identities do different jobs, and mixing them up is the most common cause of tool authorization failures.

| Identity | Who it is | What it does |
| --- | --- | --- |
| Project managed identity | One per project, type `ManagedIdentity` | Authenticates the platform: model inference through the project endpoint, pulling container images, reading telemetry. It also authenticates the blueprint to Entra through a federated credential, so no secret is stored. |
| Agent identity | One per agent, type `ServiceIdentity` | Authenticates the agent to the tools it calls. It holds the role assignments for those tools. |

The managed identity authenticates the blueprint; it does not reach the downstream resource. The agent identity is the principal that needs the role on the target resource.

## Two flows: attended and unattended

The agent identity supports two authentication scenarios.

| Scenario | OAuth flow | Whose permissions apply |
| --- | --- | --- |
| Unattended | Client credentials | The agent's own role assignments |
| Attended | On-behalf-of | The signed-in user's delegated permissions, so the agent reaches only what that user may reach |

At runtime Agent Service handles the token exchange in four stages: it authenticates the blueprint, receives a token for the agent identity, asks Entra for an access token scoped to the audience of the downstream service, and passes that token to the MCP server or A2A endpoint. The audience is the resource identifier of the service, for example `https://storage.azure.com` or `https://vault.azure.net`, not the URL of the MCP server. A wrong audience fails even when the roles are correct.

## Choose the authentication type on the connection

Authentication is a property of the project connection, never of agent code. You choose it once with `azd ai connection create`, and Foundry handles token acquisition, exchange and refresh.

| `--auth-type` | Whose identity reaches the tool |
| --- | --- |
| `none` | Anonymous |
| `custom-keys` | A stored API key or header |
| `project-managed-identity` | The project's managed identity |
| `agentic-identity` | The agent's own identity |
| `oauth2` | The user who completes OAuth authorization |
| `user-entra-token` | The signed-in Microsoft Entra user, with an audience-specific token |

```bash
azd ai connection create orders-agent-conn \
  --kind remote-tool \
  --target https://orders-mcp.example.com/mcp \
  --auth-type agentic-identity \
  --audience https://storage.azure.com
```

An `agentic-identity` connection rejects a user token on purpose. You then give the agent identity the role on the target resource, and only that agent can reach it.

## OAuth identity passthrough

Use `oauth2` when each user must keep their own permissions at the tool. You bring an OAuth app, or use a managed connector, and the connection stores the endpoints.

```bash
azd ai connection create orders-mcp \
  --kind remote-tool \
  --target https://orders-mcp.example.com/mcp \
  --auth-type oauth2 \
  --authorization-url https://auth.example.com/authorize \
  --token-url https://auth.example.com/token \
  --client-id <oauth-client-id> \
  --client-secret <oauth-client-secret> \
  --scopes "orders.read offline_access"
```

Separate scopes with single spaces, not commas, and include `offline_access` so the token refreshes. With custom OAuth you receive a redirect URL after configuration and add it to your OAuth app. The agent references the connection by name in an `mcp` tool.

```python
MCPTool(
    server_label="orders",
    server_url="https://orders-mcp.example.com/mcp",
    require_approval="never",
    project_connection_id="orders-mcp",
)
```

## The consent round trip

The first time a user needs a tool, the response contains an `oauth_consent_request` item instead of a tool result. Your application shows the `consent_link` to the user. After the user signs in and consents, you send a second response that continues from the first.

```python
for item in response.output:
    if item.type == "oauth_consent_request":
        print(f"Open this URL to authorize access: {item.consent_link}")

stream = openai_client.responses.create(
    previous_response_id=response.id,
    input=user_input,
    stream=True,
    extra_body={"agent_reference": {"name": agent_name, "type": "agent_reference"}},
)
```

Consent is scoped per tool connection per project and per user, and it is asked once. Users who call the agent need at least the Foundry Agent Consumer role on the project, which grants only the permission to interact with agent endpoints. Their Microsoft Entra tenant must match the tenant of the project; cross-tenant token exchange is not supported.

## Least privilege between agents

The Foundry Agent Consumer role has a single data action, `Microsoft.CognitiveServices/accounts/AIServices/endpoints/interact/action`. You can assign it at the project scope for every agent endpoint, or at the scope of one agent for just that endpoint. An agent that calls another agent over A2A is a consumer like any other, and its identity is the assignee.

```bash
az role assignment create \
  --assignee-object-id <orders-agent-principal-id> \
  --assignee-principal-type ServicePrincipal \
  --role eed3b665-ab3a-47b6-8f48-c9382fb1dad6 \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.CognitiveServices/accounts/<account>/projects/<project>/agents/<target-agent>
```

Use the identity's object (principal) ID for the assignment, and the role ID rather than the name because the Foundry roles were renamed recently.

## Hands-On Demo

[Give Agents Their Own Identity and Run an OAuth Consent Flow](demo-agent-identity.md) creates two agents, inspects their Entra identities, grants one of them access to the other and triggers the OAuth consent request of an MCP tool. The finished projects are `agent-identity-py/` (Python) and `agent-identity-cs/` (C#, `dotnet run -- identity | consent`).

## Key Topics

- [Agent identity concepts in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-identity)
- [Set up authentication for Model Context Protocol (MCP) tools](https://learn.microsoft.com/azure/foundry/agents/how-to/mcp-authentication)
- [How toolbox authentication works in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/agents/how-to/tools/tool-authentication)
- [Hosted agent permissions reference](https://learn.microsoft.com/azure/foundry/agents/concepts/hosted-agent-permissions)
- [Role-based access control for Microsoft Foundry](https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry)
- [Enable incoming A2A on a Foundry agent](https://learn.microsoft.com/azure/foundry/agents/how-to/enable-agent-to-agent-endpoint)
