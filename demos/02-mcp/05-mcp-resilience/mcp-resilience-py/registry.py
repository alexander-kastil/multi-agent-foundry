import random
import time
from collections import deque
from dataclasses import dataclass, field
from datetime import datetime


@dataclass
class ToolInstance:
    endpoint: str
    version: str
    impl: object
    health_status: str = "healthy"
    samples: deque = field(default_factory=lambda: deque(maxlen=20))
    last_updated: datetime = field(default_factory=datetime.now)

    @property
    def p95_latency_ms(self):
        if not self.samples:
            return 0.0
        ordered = sorted(self.samples)
        return ordered[min(len(ordered) - 1, int(len(ordered) * 0.95))]


class ToolRegistry:
    def __init__(self):
        self._tools = {}

    def register_instance(self, tool_name, endpoint, version, impl, health_status="healthy"):
        self._tools.setdefault(tool_name, []).append(ToolInstance(endpoint, version, impl, health_status))

    def instances(self, tool_name):
        return self._tools.get(tool_name, [])

    def update_health(self, tool_name, endpoint, health_status):
        for instance in self._tools.get(tool_name, []):
            if instance.endpoint == endpoint:
                instance.health_status = health_status
                instance.last_updated = datetime.now()


def version_tuple(version):
    return tuple(int(part) for part in version.split("."))


class ToolRouter:
    def __init__(self, registry):
        self.registry = registry
        self.weights = {}
        self.decisions = []

    def set_weights(self, tool_name, endpoint_weights):
        self.weights[tool_name] = endpoint_weights

    def viable(self, tool_name, minimum_version=None):
        instances = [i for i in self.registry.instances(tool_name) if i.health_status in ("healthy", "degraded")]
        if minimum_version:
            instances = [i for i in instances if version_tuple(i.version) >= version_tuple(minimum_version)]
        return instances

    def select(self, tool_name, minimum_version=None):
        instances = self.viable(tool_name, minimum_version)
        if not instances:
            return None, None
        if tool_name in self.weights:
            weights = [self.weights[tool_name].get(i.endpoint, 0) for i in instances]
            if sum(weights) > 0:
                return random.choices(instances, weights=weights)[0], "weighted"
        unmeasured = [i for i in instances if not i.samples]
        if unmeasured:
            return unmeasured[0], "latency_based_warmup"
        return min(instances, key=lambda i: i.p95_latency_ms), "latency_based"

    def resolve_and_call(self, tool_name, minimum_version=None, **kwargs):
        instance, strategy = self.select(tool_name, minimum_version)
        if instance is None:
            return {"status": "error", "message": f"No healthy instances available for {tool_name}"}
        self.decisions.append(
            {"event": "tool_routing", "tool_name": tool_name, "selected_endpoint": instance.endpoint, "routing_strategy": strategy}
        )
        started = time.perf_counter()
        try:
            return instance.impl.lookup(**kwargs)
        finally:
            instance.samples.append((time.perf_counter() - started) * 1000)
