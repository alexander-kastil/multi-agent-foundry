import random
import time
from collections import defaultdict
from foundry import create_agent, openai_client, delete_agent

WINDOW_SECONDS = 6

TIERS = {
    "premium": {"tokens_per_minute": 60000, "monthly_quota": 2000000},
    "standard": {"tokens_per_minute": 25000, "monthly_quota": 750000},
    "developer": {"tokens_per_minute": 150, "monthly_quota": 100000},
}

RATE_PER_1K = {"input": 0.0025, "output": 0.01}
SHARED_COMPUTE_COST = 12.0


class TokenLimitGateway:
    def __init__(self):
        self.window_start = defaultdict(float)
        self.window_tokens = defaultdict(int)
        self.monthly_tokens = defaultdict(int)

    def admit(self, tenant, tier, prompt):
        limits = TIERS[tier]
        now = time.monotonic()
        if now - self.window_start[tenant] >= WINDOW_SECONDS:
            self.window_start[tenant] = now
            self.window_tokens[tenant] = 0
        estimate = len(prompt) // 4
        if self.monthly_tokens[tenant] + estimate > limits["monthly_quota"]:
            return 403, {"error": "token_quota_exceeded"}
        if self.window_tokens[tenant] + estimate > limits["tokens_per_minute"] or self.window_tokens[tenant] >= limits["tokens_per_minute"]:
            return 429, {"error": "token_rate_limit_exceeded", "retry_after": WINDOW_SECONDS}
        return 200, {}

    def record(self, tenant, tokens):
        self.window_tokens[tenant] += tokens
        self.monthly_tokens[tenant] += tokens


class QuotaAwareAgentClient:
    def __init__(self, gateway, agent, usage_log, max_retries=4):
        self.gateway = gateway
        self.agent = agent
        self.usage_log = usage_log
        self.max_retries = max_retries

    def invoke(self, tenant, tier, prompt):
        for attempt in range(self.max_retries):
            status, body = self.gateway.admit(tenant, tier, prompt)
            if status == 200:
                response = openai_client.responses.create(
                    input=prompt,
                    extra_body={"agent_reference": {"name": self.agent.name, "version": self.agent.version, "type": "agent_reference"}},
                )
                usage = response.usage
                self.gateway.record(tenant, usage.total_tokens)
                self.usage_log.append((tenant, usage.input_tokens, usage.output_tokens))
                return response.output_text
            if status == 403:
                return body
            delay = body["retry_after"] * random.uniform(0.5, 1.0) * (2 ** attempt)
            print(f"    429 for {tenant}, attempt {attempt + 1}, retrying in {delay:.1f}s")
            time.sleep(delay)
        return {"error": "quota_exceeded_persistent", "tenant_id": tenant}


agent = create_agent(
    "m8-life-quota",
    "You answer security questions about source code in two sentences.",
)
gateway = TokenLimitGateway()
usage_log = []
client = QuotaAwareAgentClient(gateway, agent, usage_log)

workload = [
    ("premium-fabrikam-internal", "premium", "What is SQL injection?"),
    ("premium-fabrikam-internal", "premium", "What is cross-site scripting?"),
    ("standard-contoso", "standard", "What is a hardcoded credential?"),
    ("developer-startup", "developer", "What is insecure deserialization?"),
    ("developer-startup", "developer", "What is path traversal?"),
    ("developer-startup", "developer", "What is server-side request forgery?"),
]

print("Invocations through the token-limit gateway")
for tenant, tier, prompt in workload:
    print(f"  {tenant} ({tier}): {prompt}")
    result = client.invoke(tenant, tier, prompt)
    print(f"    -> {str(result)[:90]}")

print("\nChargeback (model cost from metered tokens, shared compute split evenly across active tenants)")
by_tenant = defaultdict(lambda: [0, 0])
for tenant, tokens_in, tokens_out in usage_log:
    by_tenant[tenant][0] += tokens_in
    by_tenant[tenant][1] += tokens_out
shared = SHARED_COMPUTE_COST / len(by_tenant)
total = 0.0
for tenant, (tokens_in, tokens_out) in sorted(by_tenant.items()):
    model_cost = tokens_in / 1000 * RATE_PER_1K["input"] + tokens_out / 1000 * RATE_PER_1K["output"]
    total += model_cost + shared
    print(f"  {tenant:<26} in={tokens_in:>5} out={tokens_out:>5} model=${model_cost:.4f} shared=${shared:.2f} total=${model_cost + shared:.4f}")
print(f"  total allocated: ${total:.4f}")

delete_agent(agent)
