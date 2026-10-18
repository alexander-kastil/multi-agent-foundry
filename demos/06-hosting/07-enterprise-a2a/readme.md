# Enterprise A2A: Discovery, Shared State & Conflicts

One A2A call between two agents is a solved problem: read the agent card, send a message, get a task back. Twenty agents owned by six teams, serving three client tenants, is a different problem. Somebody has to answer which agent is allowed to take this work, where the shared result lives, whose data it is, and what happens when two agents disagree.

This topic covers the four building blocks an A2A network needs once it leaves the single-demo stage. The protocol itself, agent cards and endpoints, is taught in [A2A Protocol](../06-a2a-protocol/readme.md); this topic starts where that one stops.

## From a card to a registry

An agent card describes one agent. A registry holds the cards of all of them and answers the routing question: which agents offer this capability right now.

In A2A v1 the card carries its endpoints in `supported_interfaces` (Python, a2a-sdk) or `SupportedInterfaces` (.NET). The card is served from `/.well-known/agent-card.json`, and a client reads it with the SDK's card resolver.

```python
card = {
    "name": "market-analysis-v2-eastus",
    "version": "2.3.1",
    "skills": [{"id": "sector-analysis", "name": "sector-analysis"}],
    "supported_interfaces": [{"url": "https://market-eus.contoso.com", "protocol_binding": "JSONRPC"}],
}
```

```csharp
AgentCard discovered = await new A2ACardResolver(new Uri("http://127.0.0.1:18901")).GetAgentCardAsync();
```

The registry turns each card into an entry with the fields routing needs: capabilities from the skills, an endpoint from the first interface, and the health data a card never carries.

```json
{
  "agentId": "market-analysis-v2-eastus",
  "endpointUrl": "https://market-eus.contoso.com",
  "capabilities": ["sector-analysis", "trend-forecasting"],
  "healthStatus": "healthy",
  "lastHeartbeat": 1792306800.0,
  "expiresAt": 1792307100.0
}
```

An entry qualifies for a capability query only when three conditions hold at once: the status is `healthy`, the last heartbeat is younger than the staleness window (120 seconds here), and the entry has not passed its time to live (300 seconds). A crashed agent therefore leaves the results without anyone deregistering it, and an agent that sets `draining` stops receiving new work while it finishes the old.

Among the qualifying agents, routing is a policy choice. The demo uses round robin over a sorted candidate list so the result is reproducible; production registries add latency, region affinity or load.

![Three registry entries feed a sector-analysis query that checks the capability, a healthy status, a heartbeat younger than 120 s and a 300 s time to live, next to the four results lab Step 1 prints](../pptx/images/07-01-capability-routing.svg)

The same idea exists as a managed service. [Foundry Control Plane](https://learn.microsoft.com/azure/foundry/control-plane/register-custom-agent) can register an agent that runs outside Agent Service, select A2A as its protocol, and discover its card at `/.well-known/agent-card.json`.

## Shared state without lost updates

Agents that work on one task need a place to put their partial results. A shared document per task works, as long as two agents writing at the same moment cannot silently overwrite each other.

Optimistic concurrency solves this with a version tag. Every read returns the document and its `_etag`; every write sends that etag back, and the store accepts the write only if the document is still at that version. Azure Cosmos DB implements exactly this: a stale `if-match` header is rejected with HTTP 412 Precondition Failed, and the client re-reads and retries.

![Sequence of two agents writing one shared document: the first write with etag 1 is accepted, the second with the stale etag gets 412 Precondition Failed, reads again and succeeds with etag 2](../pptx/images/07-02-etag-writes.svg)

```python
def contribute(self, tenant_id, task_id, agent_id, contribution, max_retries=3):
    for attempt in range(1, max_retries + 1):
        document = self.read(tenant_id, task_id, use_cache=False)
        document["contributions"][agent_id] = {"data": contribution, "version": len(document["contributions"]) + 1}
        try:
            self.replace(tenant_id, task_id, document, document["_etag"])
            return attempt
        except PreconditionFailed:
            time.sleep(0.01)
    raise PreconditionFailed("retries exhausted")
```

The retry loop re-reads on every attempt, because a retry that reuses the old document would write the very stale state the etag was meant to catch. Reads that skip the cache are the price of that guarantee, so the store also caches normal reads and publishes an invalidation message after each successful write.

The demo keeps the documents in memory so it runs without a database. The contract (`read`, `replace` with an etag, 412 on mismatch) is the Cosmos DB contract, so the in-memory class is swapped for the Cosmos client without touching the agents that call it. The Learn module on [optimistic concurrency](https://learn.microsoft.com/azure/cosmos-db/database-transactions-optimistic-concurrency) describes the header semantics the class imitates.

## Tenant isolation

When one agent network serves several clients, the tenant is part of every key. The demo builds the document id as `tenantId:taskId`, uses the same pair as the cache key, and answers task listings by filtering on the tenant. In Cosmos DB the tenant id is the natural partition key, which makes the isolation physical as well as logical.

```python
tenant_context.set({"clientId": "acme-retirement"})
own = store.read(tenant_context.get()["clientId"], "tech-analysis")
```

The tenant comes from a request-scoped context (`ContextVar` in Python, `AsyncLocal<string?>` in .NET), set once where the request enters and read where the store is called. Agents never pass a tenant id they chose themselves, so a prompt-injected agent cannot ask for another client's task.

## Conflicts between agents

Two agents working on the same question can disagree. The network needs a policy that notices, decides, and leaves evidence.

![Two cases run through the rule-based tier, the semantic tier, the priority ranks and the audit trail: case 1 is resolved for risk-v1-eastus without a model call, case 2 is judged by the model and escalated](../pptx/images/07-03-conflict-resolution.svg)

Detection runs in two tiers. A rule-based tier is free and exact: if one recommendation says `increase` or `buy` and the other says `decrease` or `sell`, the outputs contradict, with confidence 1.0. When the structured fields do not settle it, a semantic tier asks a model to judge the two analyses and return a verdict.

```json
{"status": "CONSISTENT | INCONSISTENT | COMPLEMENTARY", "reasoning": "...", "confidence": 0.0}
```

Resolution is priority based. Each agent type has a rank (`compliance` 100, `risk` 75, `market-analysis` 50, `reporting` 25) and the higher rank wins. Equal ranks cannot be settled by rank, so the result is `escalated` to an orchestrator synthesis, with a human review flag when confidence is below 0.5.

Every decision lands in an audit trail: tenant, conflict type, the two agents, confidence, resolution, method, the chosen agent, and a snapshot of both outputs. The trail is what lets a reviewer reconstruct why one agent's answer overrode another's.

A model judgement is never a fixed result. The semantic tier can rate the same pair differently on another run, so the audit trail records the confidence next to the verdict.

## Hands-On Demo

| Demo | What you run |
|------|--------------|
| [Build an Enterprise A2A Network](demo-enterprise-a2a.md) | A discovery registry, an ETag-guarded shared state store, tenant isolation and a conflict resolver, in Python and in C# |

## Key Topics

- [Design enterprise-scale agent communication with A2A in Azure](https://learn.microsoft.com/training/modules/aaai-design-enterprise-agent-communication-a2a/)
- [Host agents with A2A (.NET)](https://learn.microsoft.com/agent-framework/hosting/self-hosting/a2a/dotnet)
- [Host agents with A2A (Python)](https://learn.microsoft.com/agent-framework/hosting/self-hosting/a2a/server)
- [A2A agent service: discovery and invocation](https://learn.microsoft.com/agent-framework/integrations/by-component/agent-services/a2a)
- [A2A SDK v1 migration guide](https://learn.microsoft.com/agent-framework/migration-guide/agent-to-agent-sdk-v1)
- [Register a custom agent in Foundry Control Plane](https://learn.microsoft.com/azure/foundry/control-plane/register-custom-agent)
- [Transactions and optimistic concurrency control in Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/database-transactions-optimistic-concurrency)
