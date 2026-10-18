import time
from collections import deque
from datetime import datetime, timezone


class Gateway:
    def __init__(self, name, healthy=True):
        self.name = name
        self.healthy = healthy

    def health(self):
        return self.healthy

    def test_transaction(self):
        return self.healthy

    def charge(self, amount):
        if not self.healthy:
            return {"status": "error", "gateway": self.name}
        return {"status": "paid", "gateway": self.name, "amount": amount}


class CircuitBreaker:
    def __init__(self, window=50, failure_threshold=0.4, probe_interval_seconds=30):
        self.results = deque(maxlen=window)
        self.failure_threshold = failure_threshold
        self.probe_interval_seconds = probe_interval_seconds
        self.state = "closed"
        self.opened_at = 0.0

    def call(self, primary, fallback):
        if self.state == "open" and time.monotonic() - self.opened_at >= self.probe_interval_seconds:
            self.state = "half_open"
        if self.state == "open":
            return fallback()
        result = primary()
        success = result["status"] != "error"
        if self.state == "half_open":
            self.state = "closed" if success else "open"
            self.opened_at = time.monotonic()
            self.results.clear()
            return result if success else fallback()
        self.results.append(success)
        failures = self.results.count(False)
        if len(self.results) >= 10 and failures / len(self.results) > self.failure_threshold:
            self.state = "open"
            self.opened_at = time.monotonic()
        return result if success else fallback()


class GatewayRunbook:
    def __init__(self, primary, backup, config, max_runs_per_hour=3, deadline_seconds=300):
        self.primary = primary
        self.backup = backup
        self.config = config
        self.max_runs_per_hour = max_runs_per_hour
        self.deadline_seconds = deadline_seconds
        self.run_times = []
        self.audit = []

    def log(self, message):
        self.audit.append(f"{datetime.now(timezone.utc).isoformat()} {message}")

    def run(self):
        started = time.monotonic()
        recent = [moment for moment in self.run_times if started - moment < 3600]
        if len(recent) >= self.max_runs_per_hour:
            self.log("rate limit reached")
            return {"status": "escalation_required", "reason": "rate_limited"}
        self.run_times.append(started)
        if self.config["payment_gateway"] == self.backup.name:
            self.log("already failed over")
            return {"status": "no_action_needed", "reason": "already_on_backup"}
        failures = sum(not self.primary.health() for _ in range(3))
        if failures < 3:
            self.log("primary recovered during verification")
            return {"status": "no_action_needed", "reason": "primary_healthy"}
        if not self.backup.health():
            self.log("both gateways unhealthy")
            return {"status": "escalation_required", "reason": "both_gateways_unhealthy"}
        self.config["payment_gateway"] = self.backup.name
        self.log(f"failed over from {self.primary.name} to {self.backup.name}")
        if time.monotonic() - started > self.deadline_seconds:
            return {"status": "escalation_required", "reason": "deadline_exceeded"}
        if self.backup.test_transaction():
            return {"status": "failover_successful", "action": "notify_on_call"}
        return {"status": "failover_verification_failed", "action": "page_on_call_urgent"}


def classify_severity(checkout_error_rate, agents_incorrect_rate=0.0, affected_traffic=1.0):
    if checkout_error_rate > 0.05:
        return "P1"
    if checkout_error_rate > 0.01 or agents_incorrect_rate > 0.10:
        return "P2"
    if affected_traffic < 0.05 or agents_incorrect_rate > 0:
        return "P3"
    return "P4"
