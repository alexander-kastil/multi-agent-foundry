# Zero Trust: Tenant Isolation & Secrets

A multi-tenant agent holds other people's data and other systems' credentials, so it cannot trust anything it is told, including by the model that drives it. This topic applies Zero Trust to two risks: a request that tries to reach another tenant's data, and a credential that leaks or must change. The project in `zero-trust-py/` picks an authentication flow per scenario, rotates a secret without downtime, and proves that an agent cannot read a review that belongs to another tenant.

## Three principles for an agent

Microsoft's Zero Trust model rests on three principles: verify explicitly, use least privilege access, and assume breach. For an agent they translate into three habits.

| Principle | What it means for the agent |
| --- | --- |
| Verify explicitly | Validate the caller's token on every request before any data access |
| Use least privilege access | Give each agent only the secrets and roles it needs, and prefer identities over keys |
| Assume breach | Partition data per tenant and make rotation routine, so one leak has a small blast radius |

## Choose the authentication flow per call

Credentials differ by who the agent acts for. `auth_flows.py` prints the decision table that the rest of the topic builds on.

| Scenario | Flow |
| --- | --- |
| Agent calls an Azure service such as Cosmos DB, Foundry or Storage | Managed identity |
| Agent calls another agent inside the same Azure trust boundary | Managed identity and workload identity |
| Agent reads a user's Azure DevOps data | On-behalf-of (OBO) |
| Agent reads third-party SaaS data for a user | OAuth 2.0 authorization code with PKCE |
| Agent calls a partner API that accepts only API keys | Key-based fallback with Key Vault |

With the on-behalf-of flow the agent works with the signed-in user's delegated permissions, so it can reach only what that user may reach. Managed identities remove the secret altogether, which is why keys are the last row. The OBO flow for Foundry agents is the subject of [Entra Agent ID](../03-agent-identity/readme.md).

## Rotate a key without downtime

When a partner API forces you to hold a key, store two of them and let the consumer fall back. The rotation order never leaves a moment with no valid key: write the new secondary, disable the primary, then restore the primary with the new value.

```python
class KeyVaultSecretManager:
    def get_current_api_key(self, secret_name):
        primary = self.vault.get_secret(f"{secret_name}-primary", self.identity)
        if primary["enabled"]:
            return primary["value"]
        return self.vault.get_secret(f"{secret_name}-secondary", self.identity)["value"]
```

The project uses an in-memory `InMemoryVault` so the demo needs no Key Vault. The real client has the same shape.

```python
from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient

client = SecretClient(vault_url="https://<vault-name>.vault.azure.net", credential=DefaultAzureCredential())
secret = client.get_secret("style-api-key-primary")
```

Key Vault's own rotation guidance for services with two credentials alternates between them in successive secret versions, and a function triggered by the near-expiry event does the swap. The primary and secondary pair in the demo is a simpler model of the same idea.

## Give each agent only its own secrets

The vault in the demo refuses a read when the calling identity has no assignment for that secret. The style checker can read its own key and is denied the compliance checker's.

```python
if f"secrets/{name.rsplit('-', 1)[0]}" not in ROLES[identity]:
    raise PermissionError(f"{identity} has no Key Vault Secrets User assignment for {name}")
```

In Azure the same rule is a role assignment scoped to one secret.

```bash
az role assignment create --role "Key Vault Secrets User" --assignee <agent-identity> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<group>/providers/Microsoft.KeyVault/vaults/<vault>/secrets/<secret>
```

Microsoft recommends one vault per application per environment with roles at vault scope, and keeps secret-scope assignments for limited cases such as sharing one secret between applications. Treat secret-scope roles as a precision tool, not the default, and note that object-scope roles do not isolate application teams from vault-level administrators.

## Derive the tenant from a verified token

An agent must never learn its tenant from the model or from a request field. `tenant_context.py` verifies a signed token, reads the tenant from the verified claims, and stores it in a context variable for the rest of the request.

```python
decoded = jwt.decode(
    token,
    public_key,
    algorithms=["RS256"],
    audience=EXPECTED_AUDIENCE,
    issuer=EXPECTED_ISSUER,
)
return decoded["tenant_id"]
```

`jwt.decode` rejects a token with a bad signature, a wrong audience, a wrong issuer or an expired `exp`. The demo signs tokens with a local RSA key; a real service validates Microsoft Entra tokens against the published signing keys, checks the `aud` and `tid` claims, and uses `tid` as part of the key to the data.

```python
current_tenant = ContextVar("current_tenant", default=None)
```

## Partition the data by tenant

Every repository method reads the tenant from the context variable instead of taking it as an argument, so no caller can ask for a different tenant.

```python
def get_review(self, review_id):
    tenant_id = current_tenant.get()
    if review_id not in self.store.partitions.get(tenant_id, {}):
        return None
    return self.store.read_item(review_id, partition_key=tenant_id)
```

The store mirrors a database partition key: an item is reachable only through the partition of its tenant. A lookup of another tenant's id finds nothing and returns `None`.

## Tools that cannot be talked out of isolation

The agent receives two function tools, `list_reviews` and `get_review`. Neither has a tenant parameter, so the model has nothing to spoof.

```python
FunctionTool(
    name="get_review",
    description="Fetch one code review by id from the current tenant.",
    parameters={
        "type": "object",
        "properties": {"review_id": {"type": "string", "description": "Review id, e.g. rev-a-001."}},
        "required": ["review_id"],
        "additionalProperties": False,
    },
    strict=True,
)
```

When the user asks for a review of another tenant by id, the tool runs with the verified tenant and returns `null`. Isolation holds even if the model follows the user's instruction perfectly.

## Platform-level isolation for hosted agents

Foundry applies a second layer for hosted agents. The platform identifies each caller from the Microsoft Entra token and keeps that caller's conversations, sessions and stored data private, even though everyone uses one endpoint. A trusted middle-tier service that authenticates its own users can pass the `x-ms-user-identity` header, which needs a custom role because no built-in role includes the permission.

```python
openai_client = project.get_openai_client(agent_name="my-agent")
response = openai_client.responses.create(
    input="Summarize my open tickets",
    extra_headers={"x-ms-user-identity": "<stable-end-user-id>"},
)
```

Derive that value from an authenticated server-side identity, never from something the browser supplies. Application-level partitioning, as in this topic, stays necessary for the data your own tools read.

## Hands-On Demo

[Isolate Tenants and Rotate Secrets in a Multi-Tenant Agent](demo-zero-trust.md) runs the flow table, the rotation, the secret-level denial and a live agent that is asked for another tenant's review. The finished projects are `zero-trust-py/` (Python) and `zero-trust-cs/` (C#, `dotnet run -- flows | isolation`).

## Key Topics

- [Zero Trust security in Azure](https://learn.microsoft.com/azure/security/fundamentals/zero-trust)
- [Secure applications and APIs by validating claims](https://learn.microsoft.com/entra/identity-platform/claims-validation)
- [Access tokens in the Microsoft identity platform](https://learn.microsoft.com/entra/identity-platform/access-tokens)
- [Automate the rotation of a secret for resources that have two sets of authentication credentials](https://learn.microsoft.com/azure/key-vault/secrets/tutorial-rotation-dual)
- [Provide access to Key Vault keys, certificates, and secrets with Azure RBAC](https://learn.microsoft.com/azure/key-vault/general/rbac-guide)
- [Isolate hosted agent sessions per user](https://learn.microsoft.com/azure/foundry/agents/how-to/isolate-sessions-per-user)
- [Agent identity concepts in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-identity)
