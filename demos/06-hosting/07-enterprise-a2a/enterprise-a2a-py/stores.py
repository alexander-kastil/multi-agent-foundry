import copy
import itertools
import threading
import time
from datetime import datetime, timezone


class PreconditionFailed(Exception):
    pass


class AgentRegistry:
    def __init__(self, ttl_seconds: int = 300, stale_seconds: int = 120):
        self.ttl_seconds = ttl_seconds
        self.stale_seconds = stale_seconds
        self.items: dict[str, dict] = {}
        self.round_robin = itertools.count()

    def register(self, agent_card: dict) -> None:
        self.items[agent_card["name"]] = {
            "agentId": agent_card["name"],
            "endpointUrl": agent_card["supported_interfaces"][0]["url"],
            "capabilities": [skill["id"] for skill in agent_card["skills"]],
            "healthStatus": "healthy",
            "lastHeartbeat": time.time(),
            "version": agent_card.get("version", "1.0.0"),
            "expiresAt": time.time() + self.ttl_seconds,
        }

    def heartbeat(self, agent_id: str, health: str = "healthy") -> None:
        item = self.items[agent_id]
        item["lastHeartbeat"] = time.time()
        item["expiresAt"] = time.time() + self.ttl_seconds
        item["healthStatus"] = health

    def find_by_capability(self, capability: str) -> list[dict]:
        now = time.time()
        return [
            item
            for item in self.items.values()
            if capability in item["capabilities"]
            and item["healthStatus"] == "healthy"
            and now - item["lastHeartbeat"] < self.stale_seconds
            and item["expiresAt"] > now
        ]

    def route(self, capability: str) -> dict:
        candidates = sorted(self.find_by_capability(capability), key=lambda item: item["agentId"])
        return candidates[next(self.round_robin) % len(candidates)]


class SharedStateStore:
    def __init__(self):
        self.documents: dict[tuple[str, str], dict] = {}
        self.cache: dict[tuple[str, str], dict] = {}
        self.etag_counter = itertools.count(1)
        self.subscribers = []
        self.lock = threading.Lock()
        self.cosmos_reads = 0

    def subscribe(self, callback) -> None:
        self.subscribers.append(callback)

    def create(self, tenant_id: str, task_id: str) -> None:
        with self.lock:
            self.documents[(tenant_id, task_id)] = {
                "id": f"{tenant_id}:{task_id}",
                "tenantId": tenant_id,
                "taskId": task_id,
                "contributions": {},
                "_etag": str(next(self.etag_counter)),
            }

    def read(self, tenant_id: str, task_id: str, use_cache: bool = True) -> dict:
        key = (tenant_id, task_id)
        if use_cache and key in self.cache:
            return copy.deepcopy(self.cache[key])
        with self.lock:
            document = copy.deepcopy(self.documents[key])
            self.cosmos_reads += 1
        self.cache[key] = copy.deepcopy(document)
        return document

    def replace(self, tenant_id: str, task_id: str, document: dict, etag: str) -> None:
        key = (tenant_id, task_id)
        with self.lock:
            if self.documents[key]["_etag"] != etag:
                raise PreconditionFailed(f"etag mismatch for {tenant_id}:{task_id}")
            document["_etag"] = str(next(self.etag_counter))
            self.documents[key] = copy.deepcopy(document)
        self.cache.pop(key, None)
        for callback in self.subscribers:
            callback({"tenantId": tenant_id, "taskId": task_id, "reason": "state-update"})

    def replace_if_unmodified(self, tenant_id: str, task_id: str, document: dict, etag: str) -> bool:
        key = (tenant_id, task_id)
        if self.documents[key]["_etag"] != etag:
            return False
        self.replace(tenant_id, task_id, document, etag)
        return True

    def contribute(self, tenant_id: str, task_id: str, agent_id: str, contribution: dict, max_retries: int = 3) -> int:
        for attempt in range(1, max_retries + 1):
            document = self.read(tenant_id, task_id, use_cache=False)
            document["contributions"][agent_id] = {
                "data": contribution,
                "version": len(document["contributions"]) + 1,
                "timestamp": datetime.now(timezone.utc).isoformat(),
            }
            try:
                self.replace(tenant_id, task_id, document, document["_etag"])
                return attempt
            except PreconditionFailed:
                time.sleep(0.01)
        raise PreconditionFailed("retries exhausted")

    def query_tenant_tasks(self, tenant_id: str) -> list[str]:
        return [task_id for (tenant, task_id) in self.documents if tenant == tenant_id]
