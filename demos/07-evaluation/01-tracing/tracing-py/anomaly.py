import statistics
from datetime import datetime, timezone


class TokenUsageAnomalyDetector:
    def __init__(self, baseline_days=14, sigma_threshold=3.0):
        self.baseline_days = baseline_days
        self.sigma_threshold = sigma_threshold
        self.agent_baselines = {}

    def update_baseline(self, agent_id, daily_token_counts):
        if len(daily_token_counts) < self.baseline_days:
            return
        recent = daily_token_counts[-self.baseline_days:]
        self.agent_baselines[agent_id] = {
            "mean": statistics.mean(recent),
            "std": statistics.pstdev(recent),
            "updated_at": datetime.now(timezone.utc),
        }

    def detect_anomaly(self, agent_id, current_token_count):
        if agent_id not in self.agent_baselines:
            return {"is_anomaly": False, "reason": "No baseline established"}
        baseline = self.agent_baselines[agent_id]
        z_score = 0 if baseline["std"] == 0 else (current_token_count - baseline["mean"]) / baseline["std"]
        return {
            "is_anomaly": abs(z_score) > self.sigma_threshold,
            "z_score": z_score,
            "current_value": current_token_count,
            "baseline_mean": baseline["mean"],
            "baseline_std": baseline["std"],
            "threshold_sigma": self.sigma_threshold,
            "severity": "critical" if abs(z_score) > 3.0 else "warning",
        }


class RoutingDriftDetector:
    def __init__(self, sigma_threshold=3.0):
        self.sigma_threshold = sigma_threshold

    def detect(self, historical_shares, current_share):
        mean = statistics.mean(historical_shares)
        std = statistics.pstdev(historical_shares)
        z_score = 0 if std == 0 else (current_share - mean) / std
        return {"is_anomaly": abs(z_score) > self.sigma_threshold, "z_score": z_score, "baseline_mean": mean}


def correlate(anomalies, window_seconds=300, dependency_chain=None):
    dependency_chain = dependency_chain or {}
    incidents = []
    for anomaly in sorted(anomalies, key=lambda item: item["time"]):
        for incident in incidents:
            related_agents = {incident["agent"], *dependency_chain.get(incident["agent"], [])}
            if anomaly["agent"] in related_agents and anomaly["time"] - incident["start"] <= window_seconds:
                incident["anomalies"].append(anomaly)
                break
        else:
            incidents.append({"agent": anomaly["agent"], "start": anomaly["time"], "anomalies": [anomaly]})
    return incidents


if __name__ == "__main__":
    detector = TokenUsageAnomalyDetector()
    detector.update_baseline(
        "product-search-agent",
        [850, 920, 880, 910, 870, 900, 890, 880, 920, 870, 910, 900, 880, 890],
    )
    for usage in (905, 2650):
        result = detector.detect_anomaly("product-search-agent", usage)
        print(f"tokens={usage} z={result['z_score']:.2f} anomaly={result['is_anomaly']} severity={result['severity']}")

    drift = RoutingDriftDetector().detect([0.78, 0.75, 0.80, 0.72, 0.77, 0.74, 0.79], 0.45)
    print(f"routing share=0.45 z={drift['z_score']:.2f} anomaly={drift['is_anomaly']}")

    anomalies = [
        {"agent": "payment-agent", "metric": "latency", "time": 0},
        {"agent": "payment-agent", "metric": "error_rate", "time": 60},
        {"agent": "orchestrator-agent", "metric": "error_rate", "time": 90},
        {"agent": "payment-agent", "metric": "tokens", "time": 120},
    ]
    incidents = correlate(anomalies, dependency_chain={"payment-agent": ["orchestrator-agent"]})
    print(f"{len(anomalies)} anomalies grouped into {len(incidents)} incident(s)")
