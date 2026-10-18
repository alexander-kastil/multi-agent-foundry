import os
import sys
from foundry import connect, connect_embeddings
from router import ModelRouter
from semantic_cache import ResultCache, SemanticCache, create_redis
from context_assembler import ContextAssembler
from tradeoffs import QualityCircuitBreaker, SEGMENT_PROFILES, meets_slo, timed_calls

sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)

AGENT_ID = "support-agent"

REQUESTS = [
    "What is the status of order #12345?",
    "Where is order 12345 right now?",
    "Which gift box suits a vegan friend best: the truffle box, the praline box or the dark chocolate set?",
    "I want an exception to your return policy for damaged goods from three months ago, otherwise I will file a legal complaint.",
    "What is the status of order #12345?",
    "what's the status of my order 12345",
]

CHAIN_HISTORY = [
    {"role": "user", "content": "Hi, I am a gold member and I prefer email communication. I need to return something."},
    {"role": "assistant", "content": "Happy to help. Which order? I'll follow up by email once I have the details."},
    {"role": "user", "content": "Order #12345, the truffle box."},
    {"role": "assistant", "content": "Order 12345 was delivered 42 days ago. A return is not eligible under the 30 day policy."},
    {"role": "user", "content": "It arrived damaged though."},
    {"role": "assistant", "content": "I understand. Let me check the exception options for damaged goods."},
    {"role": "user", "content": "Please make sure to note it is for my business account."},
    {"role": "assistant", "content": "Noted for your business account. Checking now."},
    {"role": "user", "content": "How long will this take?"},
    {"role": "assistant", "content": "A reviewer will answer within four hours."},
    {"role": "user", "content": "Ok and what is the refund amount?"},
]

PRODUCT_RECORD = {"product_name": "Truffle Box", "current_price": 79.99, "stock_level": 40, **{f"field_{i}": "x" * 60 for i in range(48)}}
ORDER_RECORD = {"order_id": "12345", "fulfillment_status": "delivered", "items": [{"sku": "A"}], **{f"audit_{i}": "y" * 60 for i in range(30)}}


def routing(openai_client, embeddings_client):
    print("== Routing with multi-level cache ==")
    router = ModelRouter(openai_client)
    redis_client = create_redis()
    semantic_cache = SemanticCache(redis_client, embeddings_client)
    result_cache = ResultCache(redis_client)

    baseline_cost = 0.0
    baseline_latency = 0.0
    for question in REQUESTS:
        outcome = router.route_with_quality_floor(question, force_tier=2, max_tier=2)
        baseline_cost += outcome["cost"]
        baseline_latency += outcome["latency_s"]
    print(f"baseline, every request on tier 2 without caches: ${baseline_cost:.5f} in {baseline_latency:.1f}s")

    total_cost = 0.0
    total_latency = 0.0
    for question in REQUESTS:
        cached = result_cache.get(AGENT_ID, {"q": question})
        if cached:
            print(f"result-cache hit | {question[:60]}")
            continue
        semantic = semantic_cache.check_cache(question, AGENT_ID)
        if semantic["hit"]:
            print(f"semantic hit {semantic['similarity']:.2f} | {question[:60]}")
            continue
        outcome = router.route_with_quality_floor(question)
        total_cost += outcome["cost"]
        total_latency += outcome["latency_s"]
        semantic_cache.store_cache(question, outcome["response"], AGENT_ID, dependencies=["order-database"])
        result_cache.put(AGENT_ID, {"q": question}, outcome["response"], dependencies=["order-database"])
        print(
            f"model call tier {outcome['assigned_tier']}->{outcome['tier_used']} "
            f"quality {outcome['quality_score']} cost ${outcome['cost']:.5f} {outcome['latency_s']:.1f}s | {question[:60]}"
        )
    print(f"Result cache hits {result_cache.hits}, semantic cache hits {semantic_cache.hits}, model calls {len(REQUESTS) - result_cache.hits - semantic_cache.hits}")
    print(f"routed with caches: ${total_cost:.5f} in {total_latency:.1f}s")
    removed = semantic_cache.invalidate_by_dependency("order-database")
    print(f"Invalidated {removed} entries after order-database change")


def prefix_caching(openai_client):
    print("== Prompt prefix caching ==")
    stable_prefix = "Return policy handbook. " + " ".join(
        f"Rule {i}: refunds are issued to the original payment method within five business days." for i in range(120)
    )
    for attempt in (1, 2):
        response = openai_client.responses.create(
            model=os.environ["TIER2_MODEL"],
            instructions=stable_prefix,
            input=f"Question {attempt}: how long do refunds take? Answer in one sentence.",
        )
        print(f"call {attempt}: input tokens {response.usage.input_tokens}, cached {response.usage.input_tokens_details.cached_tokens}")


def context_budget():
    print("== Token budgets and context assembly ==")
    assembler = ContextAssembler()
    system_prompt = "You are the return eligibility agent. Apply the return policy."
    tool_results = {"product_lookup": PRODUCT_RECORD, "order_lookup": ORDER_RECORD}
    naive = assembler.count_tokens(system_prompt + str(CHAIN_HISTORY) + str(tool_results))
    for agent_type, budget in [("lookup", 120), ("reasoning", 4000)]:
        messages, used, pruned = assembler.assemble_context(CHAIN_HISTORY, system_prompt, tool_results, budget)
        print(f"{agent_type}: naive {naive} tokens -> assembled {used} tokens (budget {budget}) pruned {pruned}")


def tradeoffs():
    print("== Quality, cost and latency trade-offs ==")
    for segment, profile in SEGMENT_PROFILES.items():
        print(f"{segment}: tiers {profile['default_tiers']} history {profile['history_turns']} turns cache {profile['cache_threshold']} SLO {meets_slo(segment, 2.5, 0.004)}")
    breaker = QualityCircuitBreaker(window=10)
    for rating in [4.5, 4.0, 3.2, 3.0, 3.5, 3.1]:
        breaker.record(rating)
    print(f"Circuit breaker open: {breaker.open} settings {breaker.settings({'model_tier': 1, 'cache_enabled': True, 'history_turns': 3})}")


def parallel_calls(openai_client):
    print("== Parallel agent calls ==")
    prompts = [
        "Name one dark chocolate flavor note.",
        "Name one milk chocolate flavor note.",
        "Name one white chocolate flavor note.",
    ]
    _, sequential = timed_calls(openai_client, prompts, parallel=False)
    _, parallel = timed_calls(openai_client, prompts, parallel=True)
    print(f"3 independent agent calls: sequential {sequential:.1f}s, parallel {parallel:.1f}s")


SECTIONS = {
    "routing": lambda openai_client, embeddings_client: routing(openai_client, embeddings_client),
    "prefix": lambda openai_client, embeddings_client: prefix_caching(openai_client),
    "context": lambda openai_client, embeddings_client: context_budget(),
    "tradeoffs": lambda openai_client, embeddings_client: tradeoffs(),
    "parallel": lambda openai_client, embeddings_client: parallel_calls(openai_client),
}

if __name__ == "__main__":
    selected = sys.argv[1:] or list(SECTIONS)
    with connect() as openai_client, connect_embeddings() as embeddings_client:
        for index, name in enumerate(selected):
            if index:
                print()
            SECTIONS[name](openai_client, embeddings_client)
