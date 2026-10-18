from statistics import mean


def differential(requests):
    groups = {"successful": [], "failed": []}
    for request in requests:
        groups["successful" if request["success"] else "failed"].append(request)
    return {
        name: {
            "count": len(items),
            "avg_pricing_ms": round(mean(item["pricing_ms"] for item in items)) if items else 0,
            "currency_symbol_rate": round(sum("$" in item["price_text"] for item in items) / len(items), 2) if items else 0,
        }
        for name, items in groups.items()
    }


def first_failure_after(requests, change_time):
    failures = [request["time"] for request in requests if not request["success"] and request["time"] >= change_time]
    return min(failures) if failures else None


class HypothesisFramework:
    def __init__(self, change_log, requests, tool_latency_baseline_ms=50):
        self.change_log = change_log
        self.requests = requests
        self.tool_latency_baseline_ms = tool_latency_baseline_ms

    def changes_of(self, change_type):
        return [change for change in self.change_log if change["type"] == change_type]

    def model_deployment(self):
        changes = self.changes_of("model_deployment")
        return {"hypothesis": "model deployment change", "confirmed": bool(changes), "evidence": changes}

    def prompt_regression(self):
        changes = self.changes_of("prompt_deployment")
        if not changes:
            return {"hypothesis": "prompt regression", "confirmed": False, "evidence": []}
        change = changes[-1]
        before = [r for r in self.requests if r["time"] < change["time"]]
        after = [r for r in self.requests if r["time"] >= change["time"]]
        rate_before = sum(not r["success"] for r in before) / max(len(before), 1)
        rate_after = sum(not r["success"] for r in after) / max(len(after), 1)
        return {
            "hypothesis": "prompt regression",
            "confirmed": rate_after - rate_before > 0.2,
            "evidence": {"change": change, "failure_rate_before": rate_before, "failure_rate_after": rate_after},
        }

    def tool_failure(self):
        durations = [r["tool_ms"] for r in self.requests]
        slow = mean(durations) > 3 * self.tool_latency_baseline_ms
        return {"hypothesis": "tool failure", "confirmed": slow, "evidence": {"avg_tool_ms": round(mean(durations), 1)}}

    def orchestration_logic(self):
        changes = self.changes_of("routing_change")
        return {"hypothesis": "orchestration logic regression", "confirmed": bool(changes), "evidence": changes}

    def configuration_change(self):
        changes = self.changes_of("config_change")
        return {"hypothesis": "configuration change", "confirmed": bool(changes), "evidence": changes}

    def investigate(self):
        tested = []
        for check in (
            self.model_deployment,
            self.prompt_regression,
            self.tool_failure,
            self.orchestration_logic,
            self.configuration_change,
        ):
            result = check()
            tested.append(result)
            if result["confirmed"]:
                break
        return tested
