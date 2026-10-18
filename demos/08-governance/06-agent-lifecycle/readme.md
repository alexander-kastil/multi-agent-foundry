# Agent Lifecycle: Versions, Quotas & Retirement

An agent that serves several tenants changes over time: prompts get rewritten, models change, and old versions must eventually disappear. This topic governs that life cycle with Foundry agent versions as the unit of change.

You approve a version with a manifest, release it to tenants in waves, route traffic with the version selector on the stable endpoint, meter usage per tenant, and retire the old version with notice. The project in `agent-lifecycle-py/` implements every step and runs it against your Foundry project.

## Agent versions and the stable endpoint

A Foundry agent version is an immutable snapshot of the agent's configuration, so any change, even one prompt edit, creates a new version. The agent keeps one stable endpoint whose URL never changes while versions come and go.

```text
https://{account}.services.ai.azure.com/api/projects/{project}/agents/{agent}/endpoint/protocols/openai/responses
```

The agent's `version_selector` decides which version that endpoint serves. The default rule is `@latest`, so a new version is served the moment it exists.

Pinning sends 100 percent of the traffic to one version you name, and new versions then wait until you move the pin. Traffic splitting between versions is not supported, so the endpoint always serves exactly one version.

```python
from azure.ai.projects.models import (
    AgentEndpointConfig,
    FixedRatioVersionSelectionRule,
    VersionSelector,
)

project.agents.update_details(
    agent_name="m8-life-selector",
    agent_endpoint=AgentEndpointConfig(
        version_selector=VersionSelector(
            version_selection_rules=[FixedRatioVersionSelectionRule(agent_version="1", traffic_percentage=100)]
        )
    ),
)
```

The same change over REST is a merge patch on the agent object.

```http
PATCH {endpoint}/agents/{agent_name}?api-version=v1
Content-Type: application/merge-patch+json

{"agent_endpoint": {"version_selector": {"version_selection_rules": [
  {"type": "FixedRatio", "agent_version": "2", "traffic_percentage": 100}
]}}}
```

Promotion and rollback are the same call with a different version number. A caller that names a version in its `agent_reference` bypasses the selector, which is how you test a candidate before you promote it.

```python
client = project.get_openai_client(agent_name="m8-life-selector")
client.responses.create(input="Is eval(user_input) a vulnerability?")
```

The SDK needs `allow_preview=True` on the project client to point an OpenAI client at an agent's endpoint. An agent also stops accepting new versions after 1,000, so retire old versions rather than letting them pile up.

## The version manifest

Foundry stores what the agent is; your governance needs to record who approved it and under which rules. The manifest is a YAML file per version that sits in source control next to the prompt.

```yaml
agent_version_manifest:
  agent_id: security-analyzer
  version: 2.3.0
  system_prompt:
    version: security-prompt-v5.2
    hash: 86c1dac928c61c2f0ac94f3140b1971235a40b424712cf8a00e348882dcd2ec0
    file: prompts/security-analyzer-v5.2.txt
  evaluation_baseline:
    precision: 0.95
    recall: 0.88
  approval:
    status: approved
    approver: security-lead@fabrikam.com
  pinning_policy:
    pin_allowed: true
    max_pin_duration_days: 90
  material_change_assessment: true
```

The `hash` is a SHA-256 of the prompt file, so a prompt edited without a new manifest shows up as drift. The manifest also carries compliance fields such as a risk classification and a technical file reference, which your compliance team defines. When you publish a version to Foundry, the manifest's key facts travel as `metadata` on the agent version, which allows 16 key-value pairs of up to 64 characters for keys and 512 for values.

## Approval gate and staged rollout

Not every change needs a committee. The gate classifies a change as manual or automated by comparing two manifests.

```python
if old["model"]["deployment_name"] != new["model"]["deployment_name"]:
    reasons.append("model deployment changed")
ratio = difflib.SequenceMatcher(None, prompt_text(old).decode(), prompt_text(new).decode()).ratio()
if 1 - ratio > 0.20:
    reasons.append(f"system prompt rewritten ({1 - ratio:.0%} of text modified)")
```

A changed model, a prompt rewritten by more than 20 percent, a new tool or a lower evaluation baseline requires manual approval. Anything else, such as a temperature nudge, is automated.

Approved versions then reach tenants in tiers. Tier 1 deploys immediately, tier 2 after 7 days, and tier 3 after 30 days. A tenant with an unexpired pin stays on its pinned version until the pin expires.

| Rollout tier | Validation wait | Typical tenant |
| --- | --- | --- |
| 1 | 0 days | Internal and developer tenants |
| 2 | 7 days | Standard tenants |
| 3 | 30 days | Regulated tenants |

## Quotas and chargeback

A model deployment has a tokens-per-minute quota that all of its callers share, so one busy tenant can starve the rest. A token limit per tenant in front of the agent prevents that. In Azure API Management the `llm-token-limit` policy answers a tokens-per-minute breach with 429 and a period quota breach with 403, and Foundry exposes the same limits under Manage, AI Gateway, Token management.

```python
if self.monthly_tokens[tenant] + estimate > limits["monthly_quota"]:
    return 403, {"error": "token_quota_exceeded"}
if self.window_tokens[tenant] + estimate > limits["tokens_per_minute"] or self.window_tokens[tenant] >= limits["tokens_per_minute"]:
    return 429, {"error": "token_rate_limit_exceeded", "retry_after": WINDOW_SECONDS}
```

The project simulates that gateway in a class so you can watch the behavior without an API Management instance, and it shortens the one-minute window to 6 seconds. A 429 is retried with exponential backoff and jitter, and a 403 is final. Chargeback then prices each tenant from the tokens the Responses API reports in `response.usage` and splits a shared compute cost evenly; the rates in the code are illustrative, not Azure prices.

## Retirement

Retiring a version has a sequence: deprecate, warn, block, archive, repoint, delete. The code enforces a minimum of 90 days between the deprecation date and the deletion date, then answers requests by tenant status.

```python
if has_pin(tenant):
    return 200, {
        "result": ask(old, prompt)[:80],
        "deprecation_warning": {
            "message": f"This agent version is deprecated and will be deleted on {retirement['deletion_date']}.",
            "migration_target": retirement["replacement_agent"],
            "days_until_deletion": days_left,
        },
    }
return 410, {
    "error": "agent_deprecated",
    "message": f"Security Analyzer 2.2.0 was deprecated on {retirement['deprecated_date']} and will be deleted on {retirement['deletion_date']}.",
    "migration": {"replacement": retirement["replacement_agent"]},
    "deprecation_timeline": {"days_remaining": days_left},
}
```

A pinned tenant keeps working and sees a deprecation warning. Everyone else receives HTTP 410 with the replacement. Before deletion the code archives the manifest to `out/`, repoints the stable endpoint to the replacement, and only then calls `project.agents.delete_version`, so the endpoint never points at a version that no longer exists.

## Hands-On Demo

[Release, Pin, Meter and Retire an Agent Version](demo-agent-lifecycle.md) walks through the manifest gate, the staged rollout, the version selector, the token gateway and the retirement sequence against your Foundry project. The finished projects are `agent-lifecycle-py/` (Python) and `agent-lifecycle-cs/` (C#, `dotnet run -- governance | selector | quota | retirement`).

## Key Topics

- [Configure and share your agent: version selector and agent object model](https://learn.microsoft.com/azure/foundry/agents/how-to/configure-agent)
- [Foundry Agent Service limits, quotas, and regional support](https://learn.microsoft.com/azure/foundry/agents/concepts/limits-quotas-regions)
- [Enforce token limits for models](https://learn.microsoft.com/azure/foundry/control-plane/how-to-enforce-limits-models)
- [Manage Azure OpenAI in Microsoft Foundry Models quota](https://learn.microsoft.com/azure/foundry/openai/how-to/quota)
- [Limit large language model API token usage (llm-token-limit)](https://learn.microsoft.com/azure/api-management/llm-token-limit-policy)
- [AI gateway in Azure API Management](https://learn.microsoft.com/azure/api-management/genai-gateway-capabilities)
