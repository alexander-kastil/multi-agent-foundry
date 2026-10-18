import json
import os
import sys
from concurrent.futures import ThreadPoolExecutor
from contextvars import ContextVar

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

from conflicts import AuditTrail, detect, resolve
from stores import AgentRegistry, SharedStateStore

tenant_context: ContextVar[dict | None] = ContextVar("tenant_context", default=None)


def agent_card(name: str, url: str, skills: list[str]) -> dict:
    return {
        "name": name,
        "version": "2.3.1",
        "skills": [{"id": skill, "name": skill} for skill in skills],
        "supported_interfaces": [{"url": url, "protocol_binding": "JSONRPC"}],
    }


def demo_registry() -> None:
    print("Discovery registry and capability routing")
    registry = AgentRegistry()
    registry.register(agent_card("market-analysis-v2-eastus", "https://market-eus.contoso.com", ["sector-analysis", "trend-forecasting"]))
    registry.register(agent_card("market-analysis-v2-westeurope", "https://market-weu.contoso.com", ["sector-analysis"]))
    registry.register(agent_card("risk-v1-eastus", "https://risk-eus.contoso.com", ["portfolio-risk"]))
    print(f"sector-analysis candidates: {[a['agentId'] for a in registry.find_by_capability('sector-analysis')]}")
    print(f"round robin: {[registry.route('sector-analysis')['agentId'] for _ in range(4)]}")
    registry.heartbeat("market-analysis-v2-westeurope", "draining")
    print(f"after draining westeurope: {[a['agentId'] for a in registry.find_by_capability('sector-analysis')]}")
    registry.items["market-analysis-v2-eastus"]["lastHeartbeat"] -= 600
    print(f"after eastus heartbeat goes stale: {[a['agentId'] for a in registry.find_by_capability('sector-analysis')]}")


def demo_shared_state() -> SharedStateStore:
    print("\nShared state with ETag concurrency and cache invalidation")
    store = SharedStateStore()
    invalidations = []
    store.subscribe(invalidations.append)
    store.create("acme-retirement", "tech-analysis")
    store.read("acme-retirement", "tech-analysis")
    store.read("acme-retirement", "tech-analysis")
    print(f"two cached reads cost {store.cosmos_reads} store read(s)")
    agents = ["market-analysis-v2", "risk-v1", "compliance-v1", "reporting-v1"]
    with ThreadPoolExecutor(max_workers=len(agents)) as pool:
        attempts = list(pool.map(lambda a: store.contribute("acme-retirement", "tech-analysis", a, {"note": f"from {a}"}), agents))
    document = store.read("acme-retirement", "tech-analysis", use_cache=False)
    stale = store.read("acme-retirement", "tech-analysis", use_cache=False)
    store.contribute("acme-retirement", "tech-analysis", "esg-v1", {"note": "newer write"})
    stale["contributions"]["late-agent"] = {"data": {}, "version": 99}
    accepted = store.replace_if_unmodified("acme-retirement", "tech-analysis", stale, stale["_etag"])
    print(f"write with a stale etag accepted: {accepted}")
    print(f"attempts per agent: {attempts}, contributions kept: {sorted(document['contributions'])}")
    print(f"invalidation messages published: {len(invalidations)}")
    return store


def demo_isolation(store: SharedStateStore) -> None:
    print("\nTenant isolation")
    store.create("other-client", "tech-analysis")
    store.contribute("other-client", "tech-analysis", "market-analysis-v2", {"note": "other client view"})
    tenant_context.set({"clientId": "acme-retirement"})
    own = store.read(tenant_context.get()["clientId"], "tech-analysis")
    print(f"acme sees agents: {sorted(own['contributions'])}")
    print(f"acme task list: {store.query_tenant_tasks('acme-retirement')}")
    print(f"other-client task list: {store.query_tenant_tasks('other-client')}")
    print(f"cache keys carry the tenant: {sorted(store.cache)}")


def demo_conflicts(openai_client, model: str) -> None:
    print("\nConflict detection, resolution and audit trail")
    audit = AuditTrail()
    market = {"analysis": "MSFT shows strong growth indicators and a favorable setup.", "recommendation": {"symbol": "MSFT", "action": "increase"}}
    risk = {"analysis": "MSFT faces elevated sector concentration risk.", "recommendation": {"symbol": "MSFT", "action": "decrease"}}
    report = detect(openai_client, model, "market-analysis-v2-eastus", market, "risk-v1-eastus", risk)
    outcome = resolve(report, market, risk)
    audit.log("acme-retirement", report, outcome, {"market-analysis": market, "risk": risk})
    print(f"rule-based: conflict={report['conflict']} type={report['type']} resolution={outcome['method']} chosen={outcome['chosenAgent']}")

    bull = {"analysis": "Growth prospects are strong and valuation is justified, so we should accumulate shares.", "recommendation": {"symbol": "MSFT"}}
    bear = {"analysis": "Valuation is stretched and earnings risk is high, so we should reduce the holding.", "recommendation": {"symbol": "MSFT"}}
    report = detect(openai_client, model, "market-analysis-v2-eastus", bull, "market-analysis-v2-westeurope", bear)
    outcome = resolve(report, bull, bear)
    audit.log("acme-retirement", report, outcome, {"a": bull, "b": bear})
    print(f"semantic: conflict={report['conflict']} type={report['type']} confidence={report['confidence']} resolution={outcome['resolution']}")

    print(f"audit entries: {len(audit.entries)}")
    print(json.dumps({k: v for k, v in audit.entries[0].items() if k != "snapshots"}, indent=2))


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    load_dotenv()
    model = os.getenv("MODEL_DEPLOYMENT_NAME")
    demo_registry()
    store = demo_shared_state()
    demo_isolation(store)
    with (
        DefaultAzureCredential() as credential,
        AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=credential) as project_client,
        project_client.get_openai_client() as openai_client,
    ):
        demo_conflicts(openai_client, model)


if __name__ == "__main__":
    main()
