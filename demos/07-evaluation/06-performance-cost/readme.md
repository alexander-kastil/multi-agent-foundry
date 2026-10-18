# Performance & Cost: Routing & Caching

Agent cost has three drivers: which model answers, how many tokens go in and how often the same work is done twice. A single flagship model for every request, a prompt that grows with every turn and no reuse of earlier answers is the expensive default. Each lever below trades a little complexity for fewer tokens or less waiting, and each needs a quality guard so that savings never turn into wrong answers.

## Route by Complexity, With a Floor

Most traffic is simple. A router classifies each request into a tier and sends it to the cheapest model that can handle it: short lookups to a small model, general questions to the default model and requests with escalation signals, such as a legal threat or a large transaction, to the strongest model.

```python
def classify_complexity(self, request_text, context=None):
    lowered = request_text.lower()
    if any(keyword in lowered for keyword in ESCALATION_KEYWORDS) or context.get("transaction_amount", 0) > 500:
        return 3
    if len(request_text.split()) < 12 and re.search(r"\d", request_text):
        return 1
    return 2
```

A cheap model that answers badly costs more than it saves.

The quality floor scores each answer and retries one tier up when the score is below the tier's floor, so a bad cheap answer is escalated instead of shipped. Foundry also ships a managed router: a model router deployment picks the underlying model per prompt in Balanced, Cost or Quality mode, and reports the chosen model in the response. Use the managed router when your traffic is diverse; write your own when the routing rule is a business rule such as customer tier or order value.

## Cache at Three Levels

| Level | Matches on | Saves | Risk |
| --- | --- | --- | --- |
| Result cache | The exact normalized request | The whole model call | Stale data |
| Semantic cache | Embedding similarity above a threshold | The whole model call for a paraphrase | A wrong answer returned for a different question |
| Prompt cache | An identical prompt prefix of at least 1,024 tokens | Input processing, billed at a discount | None to correctness |

The first two are yours to build. A result cache is a key-value lookup. A semantic cache embeds the request, compares it with stored entries by cosine similarity and returns the stored answer above a threshold, so the threshold is a quality knob: raise it when a wrong cached answer is expensive.

```python
def check_cache(self, query_text, agent_id):
    query_embedding = self.get_embedding(query_text)
    ...
    if best_match is not None and best_similarity >= self.threshold:
        return {"hit": True, "response": best_match["response"], "similarity": best_similarity}
```

Tag every entry with the data it depended on and a time to live. When the order database changes, one call removes every cached entry that relied on it, which is how you keep a cache from serving yesterday's order status.

The prompt cache is provided by the service. For supported models it is on by default, needs a prefix of at least 1,024 identical tokens and reports hits as `cached_tokens` in the usage; on models before the GPT-5.6 family, hits come in 128-token steps after the first 1,024. Put stable content first and variable content last, otherwise the prefix never matches.

```python
response = openai_client.responses.create(model=deployment, instructions=stable_prefix, input=question)
print(response.usage.input_tokens_details.cached_tokens)
```

## Budget the Context

Every token in the prompt is paid for on every call. Give each agent type a token budget and assemble its context in a fixed order: a digest of established facts, the last few exchanges, older turns that carry preferences or commitments, and tool results compressed to the few fields the agent needs. Only when the result is still over budget does the assembler prune, in a defined order: shorten the window, drop important turns, drop tool results.

```python
AGENT_BUDGETS = {"lookup": 2000, "reasoning": 4000, "policy": 8000}
```

A lookup agent does not need the whole conversation, and a reasoning agent does not need a 78-field product record. Counting tokens with the model's tokenizer instead of characters keeps the budget honest.

## Set Targets per Segment and Trip a Breaker

One service level does not fit every customer. Define a profile per segment: which tiers it may use, how much history, what cache threshold, a p95 latency target and a cost limit per request. Check measured values against the profile, and add a circuit breaker on quality: when the average user rating over a recent window falls below a floor, the breaker opens and the segment falls back to the strongest tier with caching off, regardless of the optimized settings.

## Run Independent Work in Parallel

Calls that do not depend on each other cost the same tokens in any order, but latency is the sum when you wait for them one by one and the slowest call when you run them together. Fan out independent agent calls with a thread pool or `asyncio.gather` and keep sequencing only where a later step needs an earlier result.

```python
with ThreadPoolExecutor(len(prompts)) as pool:
    results = list(pool.map(call, prompts))
```

## Hands-On Demo

[Cut Agent Cost With Routing, Caching and Token Budgets](demo-performance-cost.md) measures a six-request support workload against a tier-2-only baseline, adds a result cache, a semantic cache with real embeddings and dependency invalidation, shows the service's prompt cache, enforces a token budget and checks segment targets. It ships as `performance-cost-py/` for Python and `performance-cost-cs/` for C#.

## Key Topics

- [Model router for Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/openai/concepts/model-router)
- [How model router works](https://learn.microsoft.com/en-us/azure/foundry/openai/concepts/model-router-how-it-works)
- [Use model router with Foundry agents](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/model-router-agents)
- [Prompt caching](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/prompt-caching)
- [Optimize multi-agent performance and cost](https://learn.microsoft.com/en-us/training/modules/aaai-optimize-multi-agent-performance-cost-azure/)
- [Azure OpenAI reasoning models](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/reasoning)
