import os
import re
import time

ILLUSTRATIVE_PRICE_PER_MILLION_TOKENS = {
    1: (0.25, 2.0),
    2: (2.5, 15.0),
    3: (1.25, 10.0),
}

ESCALATION_KEYWORDS = ["exception", "legal", "complaint", "lawsuit", "refund policy"]


class ModelRouter:
    def __init__(self, openai_client):
        self.openai_client = openai_client
        self.tier_deployments = {
            1: os.environ["TIER1_MODEL"],
            2: os.environ["TIER2_MODEL"],
            3: os.environ["TIER3_MODEL"],
        }
        self.quality_floors = {1: 75, 2: 85, 3: 60}

    def classify_complexity(self, request_text, context=None):
        context = context or {}
        lowered = request_text.lower()
        if any(keyword in lowered for keyword in ESCALATION_KEYWORDS) or context.get("transaction_amount", 0) > 500:
            return 3
        if len(request_text.split()) < 12 and re.search(r"\d", request_text):
            return 1
        return 2

    def evaluate_quality(self, response_text, tier):
        score = 100
        if any(phrase in response_text.lower() for phrase in ["i'm not sure", "i don't know", "unclear", "cannot determine"]):
            score -= 30
        if len(response_text.split()) < 8:
            score -= 30
        if tier == 1 and len(response_text.split()) > 150:
            score -= 10
        return max(score, 0)

    def estimate_cost(self, tier, usage):
        input_price, output_price = ILLUSTRATIVE_PRICE_PER_MILLION_TOKENS[tier]
        return (usage.input_tokens * input_price + usage.output_tokens * output_price) / 1_000_000

    def route_with_quality_floor(self, request_text, context=None, max_tier=3, force_tier=None):
        assigned_tier = force_tier or self.classify_complexity(request_text, context)
        tier = assigned_tier
        total_cost = 0.0
        started = time.perf_counter()
        while True:
            response = self.openai_client.responses.create(
                model=self.tier_deployments[tier],
                instructions="You are a concise customer service assistant for a chocolate shop. Answer in at most three sentences.",
                input=request_text,
            )
            total_cost += self.estimate_cost(tier, response.usage)
            quality = self.evaluate_quality(response.output_text, tier)
            if quality >= self.quality_floors[tier] or tier == max_tier:
                return {
                    "response": response.output_text,
                    "assigned_tier": assigned_tier,
                    "tier_used": tier,
                    "retries": tier - assigned_tier,
                    "quality_score": quality,
                    "needs_human_review": quality < self.quality_floors[tier],
                    "cost": total_cost,
                    "latency_s": time.perf_counter() - started,
                }
            tier += 1
