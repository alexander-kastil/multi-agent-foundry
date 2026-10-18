import os
import time
from concurrent.futures import ThreadPoolExecutor

SEGMENT_PROFILES = {
    "gold": {
        "default_tiers": [2, 3],
        "history_turns": 5,
        "cache_threshold": 0.93,
        "p95_latency_s": 3,
        "cost_limit": 0.015,
    },
    "standard": {
        "default_tiers": [1, 2],
        "history_turns": 3,
        "cache_threshold": 0.90,
        "p95_latency_s": 6,
        "cost_limit": 0.005,
    },
    "bulk_api": {
        "default_tiers": [1],
        "history_turns": 0,
        "cache_threshold": 0.85,
        "p95_latency_s": 12,
        "cost_limit": 0.001,
    },
}


def meets_slo(segment, p95_latency_s, average_cost):
    profile = SEGMENT_PROFILES[segment]
    return {
        "latency_ok": p95_latency_s <= profile["p95_latency_s"],
        "cost_ok": average_cost <= profile["cost_limit"],
    }


class QualityCircuitBreaker:
    def __init__(self, rating_floor=3.8, window=20):
        self.rating_floor = rating_floor
        self.window = window
        self.ratings = []
        self.open = False

    def record(self, rating):
        self.ratings.append(rating)
        recent = self.ratings[-self.window:]
        if len(recent) >= self.window // 2 and sum(recent) / len(recent) < self.rating_floor:
            self.open = True
        return self.open

    def settings(self, optimized):
        return {"model_tier": 3, "cache_enabled": False, "history_turns": 5} if self.open else optimized


def timed_calls(openai_client, prompts, parallel):
    model = os.environ["TIER1_MODEL"]

    def call(prompt):
        return openai_client.responses.create(model=model, input=prompt).output_text

    started = time.perf_counter()
    if parallel:
        with ThreadPoolExecutor(len(prompts)) as pool:
            results = list(pool.map(call, prompts))
    else:
        results = [call(prompt) for prompt in prompts]
    return results, time.perf_counter() - started
