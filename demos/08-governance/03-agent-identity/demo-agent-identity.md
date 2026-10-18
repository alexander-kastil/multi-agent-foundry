# Give Agents Their Own Identity and Run an OAuth Consent Flow

Two agents in one project often share one set of permissions, so nobody can say which agent touched which system, and a user's own access never reaches the tools. You give two agents separate Microsoft Entra identities, let one of them call the other with a least-privilege role, and make an MCP tool ask a user for consent through OAuth identity passthrough.

What you practice here: deciding whose identity reaches each tool, and granting only the role that identity needs at the narrowest scope.

Duration: about 40 minutes.

## Prerequisites

- Python 3.12 or later or the .NET 10 SDK, the Azure CLI signed in with `az login`, and the Azure Developer CLI with the Foundry extensions (`azd extension install microsoft.foundry`).
- Owner or Role Based Access Control Administrator on the project scope, so you can create the Foundry Agent Consumer role assignment. Foundry Project Manager can assign only the Foundry User role.
- Concepts from the [topic readme](readme.md): agent identity, authentication types and the consent round trip.

In Git Bash, run `export MSYS_NO_PATHCONV=1` first. Without it Git Bash rewrites resource paths that start with `/subscriptions` and `az` answers with a misleading `MissingSubscription` error.

## Run it

```bash
cd demos/08-governance/03-agent-identity/agent-identity-py
pip install -r requirements.txt
cp .env.example .env
python agent_identity.py
```

The C# twin has two entry points. It names its items `m8h-idn-`, so both projects can run side by side.

```bash
cd demos/08-governance/03-agent-identity/agent-identity-cs
cp .env.example .env
dotnet run -- identity
```

Set `PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME` and `OAUTH_CONNECTION_NAME` in `.env` first. Work in `agent-identity-py/` or `agent-identity-cs/`; both are finished projects. Python items are named `m8-idn-`.

Steps 2, 3 and 4 use `az` and `azd` only and are the same for both languages. Use the principal ids that your run printed.

## Step 1: Read each agent's identity

Creating an agent also creates its Entra identity and blueprint. The script creates an orders agent and an HR agent and prints what the platform assigned.

```python
project.agents.create_version(
    agent_name=name,
    definition=PromptAgentDefinition(model=os.getenv("MODEL_DEPLOYMENT_NAME"), instructions="You answer in one sentence."),
)
agent = project.agents.get(agent_name=name)
print(f"  agent identity principal_id: {agent.instance_identity['principal_id']}")
print(f"  blueprint reference:         {agent.blueprint_reference['blueprint_id']}")
```

Expected output:

```text
m8-idn-orders
  agent identity principal_id: 1163ead5-3942-4b08-83b0-2cf7f4fa271f
  agent identity client_id:    1163ead5-3942-4b08-83b0-2cf7f4fa271f
  blueprint reference:         m8-idn-orders-bd5f8
  inbound authorization:       Entra
m8-idn-hr
  agent identity principal_id: 65afcf5f-565f-4a49-ad18-1c7e7dae5586
  agent identity client_id:    65afcf5f-565f-4a49-ad18-1c7e7dae5586
  blueprint reference:         m8-idn-hr-920df
  inbound authorization:       Entra
```

The two agents have different principals and different blueprints. The last line shows how callers reach each endpoint: a Microsoft Entra token. Your ids will differ.

### C#

`AgentIdentity.cs` creates the agents through `CreateAgentVersionAsync` and reads the identity from the raw agent record. The typed `ProjectsAgentRecord` in `Azure.AI.Projects.Agents` 2.0.0 has no property for `instance_identity` or `blueprint_reference`.

```csharp
ClientResult agent = await project.AgentAdministrationClient.GetAgentAsync(name, new RequestOptions());
JsonNode body = JsonNode.Parse(agent.GetRawResponse().Content)!;
Console.WriteLine($"  agent identity principal_id: {body["instance_identity"]!["principal_id"]}");
```

Run `dotnet run -- identity`. Expected output:

```text
m8h-idn-orders
  agent identity principal_id: 2dae3cef-4395-45bf-b177-ca3e9be56ea2
  agent identity client_id:    2dae3cef-4395-45bf-b177-ca3e9be56ea2
  blueprint reference:         m8h-idn-orders-2ac81
  inbound authorization:       Entra
m8h-idn-hr
  agent identity principal_id: 755b5553-d131-4e25-bfc4-dd06bbada614
  agent identity client_id:    755b5553-d131-4e25-bfc4-dd06bbada614
  blueprint reference:         m8h-idn-hr-7751f
  inbound authorization:       Entra
```

## Step 2: Find the identity in Entra and compare it with the project identity

The agent identity is a real principal in your tenant. Look up the orders agent with its principal id, then look up the project's managed identity for comparison.

```bash
az rest --method get --url "https://graph.microsoft.com/v1.0/servicePrincipals/<orders-principal-id>" --query "{displayName:displayName,type:servicePrincipalType}" -o json

az rest --method get --url "https://management.azure.com/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.CognitiveServices/accounts/<account>/projects/<project>?api-version=2025-06-01" --query identity.principalId -o tsv
```

Expected output of the first command, then of the second:

```text
{
  "displayName": "pro-code-agents-foundry-pro-code-agents-m8-idn-orders-AgentIdentity",
  "type": "ServiceIdentity"
}
18a8fd0b-2a25-46d6-b9b3-2ef7e4822ea2
```

Run the first command again with the project principal id. Expected output:

```text
{
  "displayName": "pro-code-agents-foundry/projects/pro-code-agents",
  "type": "ManagedIdentity"
}
```

The agent is a `ServiceIdentity` named after the account, project and agent. The project is a `ManagedIdentity`. Tool roles belong to the first, platform roles to the second.

## Step 3: Let one agent call another with the narrowest role

The orders agent must be able to call the HR agent over its endpoint and nothing else. Foundry Agent Consumer holds only the `interact` data action, and assigning it at the scope of the HR agent limits it to that one endpoint.

```bash
az role assignment create \
  --assignee-object-id <orders-principal-id> \
  --assignee-principal-type ServicePrincipal \
  --role eed3b665-ab3a-47b6-8f48-c9382fb1dad6 \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.CognitiveServices/accounts/<account>/projects/<project>/agents/m8-idn-hr
```

Expected output:

```text
{
  "principalId": "1163ead5-3942-4b08-83b0-2cf7f4fa271f",
  "scope": "/subscriptions/.../projects/pro-code-agents/agents/m8-idn-hr"
}
```

List the assignments at that scope to confirm what you created.

```bash
az role assignment list --scope <same-agent-scope> --query "[?scope=='<same-agent-scope>'].{principalId:principalId,role:roleDefinitionName}" -o json
```

Expected output:

```text
[
  {
    "principalId": "1163ead5-3942-4b08-83b0-2cf7f4fa271f",
    "role": "Foundry Agent Consumer"
  }
]
```

The orders agent can interact with the HR agent's endpoint. It cannot create or change agents, and it has no access to other agents' endpoints.

## Step 4: Create one connection per identity model

A connection fixes whose identity reaches the tool. The first uses the agent's own identity with the audience of the target service, and the second uses OAuth so each user brings their own permissions.

```bash
azd ai connection create m8-idn-orders-agentic --kind remote-tool --target https://orders-mcp.example.com/mcp --auth-type agentic-identity --audience https://storage.azure.com -p <project-endpoint> --no-prompt

azd ai connection create m8-idn-github-oauth --kind remote-tool --target https://api.githubcopilot.com/mcp/ --auth-type oauth2 --authorization-url https://github.com/login/oauth/authorize --token-url https://github.com/login/oauth/access_token --refresh-url https://github.com/login/oauth/access_token --client-id m8-demo-client-id --client-secret m8-demo-not-a-real-secret --scopes "read:user offline_access" -p <project-endpoint> --no-prompt
```

The client id and secret are placeholders, which is enough for Foundry to start the flow and not enough to finish it. Expected output:

```text
Connection "m8-idn-orders-agentic" created in project "pro-code-agents".
Connection "m8-idn-github-oauth" created in project "pro-code-agents".
```

List them with `azd ai connection list -p <project-endpoint> --no-prompt`.

```text
Name                   Kind                 Auth Type               Target
m8-idn-orders-agentic  RemoteTool           AgenticIdentityToken    https://orders-mcp.example.com/mcp
m8-idn-github-oauth    RemoteTool           OAuth2                  https://api.githubcopilot.com/mcp/
```

Scopes are separated by a single space. With a comma between them, the tool call failed in testing with a 424 `tool_server_error` before any consent request appeared.

## Step 5: Trigger the OAuth consent request

The agent gets an `mcp` tool that points at the OAuth connection. The first call by a user returns a consent request instead of a tool result.

```python
MCPTool(
    server_label="github",
    server_url="https://api.githubcopilot.com/mcp/",
    require_approval="never",
    project_connection_id=os.getenv("OAUTH_CONNECTION_NAME"),
)
```

Run `python oauth_consent.py`. Expected output:

```text
output item: mcp_list_tools
output item: oauth_consent_request
  consent host: logic-swedencentral-001.consent.azure-apihub.net
  response id:  resp_0a38dd05224cfd32006ad463707ff88196b1a6805eb11a2de5
```

The `consent_link` on that item is what your application shows to the user. After the user signs in and consents, the application continues the conversation.

```python
stream = openai_client.responses.create(
    previous_response_id=response.id,
    input="Which GitHub user am I?",
    stream=True,
    extra_body={"agent_reference": {"name": "m8-idn-github", "type": "agent_reference"}},
)
```

This second call needs a real OAuth app and a person who completes consent, so it was not executed here. When it succeeds, the tool runs with that user's token, and later calls by the same user skip the consent step.

### C#

`OAuthConsent.cs` builds the MCP tool with `ResponseTool.CreateMcpTool` and sets `ProjectConnectionId` to the OAuth connection. The consent link is read from the raw output item, because `OAuthConsentRequestResponseItem` exposes only `ServerLabel`.

```csharp
McpTool github = ResponseTool.CreateMcpTool(
    serverLabel: "github",
    serverUri: new Uri("https://api.githubcopilot.com/mcp/"),
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
github.ProjectConnectionId = Environment.GetEnvironmentVariable("OAUTH_CONNECTION_NAME")!;
```

Run `dotnet run -- consent`. Expected output:

```text
output item: mcp_list_tools
output item: oauth_consent_request
  consent host: logic-swedencentral-001.consent.azure-apihub.net
  response id:  resp_031e98db6ca3bfcf006ad46f6900d08196bb2b37e361fab192
```

The follow-up call that continues after consent needs a real OAuth app and a person, so it was not executed in C# either.

## Verify

- Step 1 printed two different principal ids.
- Step 3 lists exactly one assignment with the role Foundry Agent Consumer at the HR agent's scope.
- Step 5 printed an `oauth_consent_request` item.

## Cleanup

Remove the role assignment, the connections and the agents.

```bash
az role assignment delete --assignee <orders-principal-id> --role eed3b665-ab3a-47b6-8f48-c9382fb1dad6 --scope <hr-agent-scope>
azd ai connection delete m8-idn-orders-agentic --force -p <project-endpoint> --no-prompt
azd ai connection delete m8-idn-github-oauth --force -p <project-endpoint> --no-prompt
python -c "import os; from dotenv import load_dotenv; load_dotenv(); from azure.identity import DefaultAzureCredential; from azure.ai.projects import AIProjectClient; p = AIProjectClient(endpoint=os.getenv('PROJECT_ENDPOINT'), credential=DefaultAzureCredential()); [p.agents.delete(agent_name=n) for n in ('m8-idn-orders', 'm8-idn-hr')]"
```

For the C# run, replace `m8-idn-orders` and `m8-idn-hr` with `m8h-idn-orders` and `m8h-idn-hr`. The consent script deletes its own agent version.
