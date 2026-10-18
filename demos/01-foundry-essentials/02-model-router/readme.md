# Model Deployments & Model Router

An agent is only as good as the model behind it, and the right model changes per request. A short lookup does not need the same model as a code review. Model router lets you deploy one model and have Foundry choose the underlying model for every call.

## Model deployments

A model deployment is a named, billable endpoint for one model on a Foundry resource. Your code never sends a model name from the catalog; it sends the deployment name. Every project on the resource sees every deployment of the resource.

```python
response = openai.responses.create(
    model="gpt-5.4",
    input="What is the capital of Austria?",
)
```

The `sku` of a deployment decides where data is processed and how it is billed. Global Standard routes across Azure regions and bills per token, Data Zone Standard keeps processing inside the US or EU data zone, and Standard stays in one region. Capacity is a quota you reserve in units of 1,000 tokens per minute.

## What model router is

Model router is a trained language model that you deploy like any other Foundry model. It reads each prompt in real time and forwards it to the most suitable model from a pool. The current version is `2025-11-18`, and Microsoft adds new models to that version without changing the version number.

The pool spans OpenAI, Anthropic, xAI, DeepSeek, Meta and Fireworks models. You do not deploy the pool models separately, with one exception covered below. You pay what the underlying model charges for the call, and the response tells you which model served it.

```python
response = openai.responses.create(
    model="m1-router-balanced",
    input="In one sentence, name the most popular tourist destination in Seattle.",
)
print(response.model)
```

The `model` field of the response carries the name of the underlying model, for example `gpt-5.6-sol-2026-07-09`. A different prompt, or the same prompt a moment later, can return a different name.

## Routing modes

The routing mode controls how the router trades quality against cost. Balanced is the default and the right start for most workloads.

| Mode | Behavior | Use it for |
| --- | --- | --- |
| Balanced (default) | Considers models within a small quality range of the best one and picks the most cost-effective | General purpose |
| Quality | Picks the highest-rated model for the prompt and ignores cost | Complex reasoning, critical output |
| Cost | Accepts a larger quality band and picks the cheapest model in it | High volume, budget-sensitive work |

You set the mode on the deployment, not on the request. Use the management API with a `routing` block; omit the block for Balanced. A mode change can take up to five minutes to apply.

```bash
az rest --method put \
  --url "https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.CognitiveServices/accounts/$ACCOUNT_NAME/deployments/m1-router-quality?api-version=2025-10-01-preview" \
  --body '{
    "sku": {"name": "GlobalStandard", "capacity": 10},
    "properties": {
      "model": {"format": "OpenAI", "name": "model-router", "version": "2025-11-18"},
      "routing": {"mode": "quality"}
    }
  }'
```

## Model subsets and failover

A model subset restricts the router to the models you list. Use it for compliance, for models with a large enough context window, or to keep cost predictable. New pool models are not added to an existing subset until you add them.

```json
"routing": {
  "mode": "balanced",
  "models": [
    {"format": "OpenAI", "name": "gpt-5-mini", "version": "2025-08-07"},
    {"format": "OpenAI", "name": "gpt-5.4", "version": "2026-03-05"}
  ]
}
```

Automatic failover is on by default. When the chosen model has a transient problem, the router redirects the request to the next most suitable model and keeps your routing mode. Your subset doubles as the fallback set, so choose at least two models to benefit from it.

## Claude models in the pool

The router can route to Anthropic Claude models, but those are the one exception to the rule that you do not deploy the pool. Deploy each Claude model you want from the model catalog first, on the same Foundry resource, and then select it for routing. A subset that names an undeployed Claude model fails with `InvalidResourceProperties`.

## Limits to design around

The effective context window is the window of the smallest model the router can pick, so use a subset when you send long prompts. The routing decision uses the text of the prompt only, even when you attach images, and audio input is not supported. Requests with `reasoning_effort` are honored when the router picks a reasoning model.

Stateless Chat Completions conversations can ask the router to prefer the same model across turns with session affinity, which is in preview and needs a `Foundry-Features: ModelRouterControls=V1Preview` header. It helps prompt caching but does not guarantee a cache hit.

## Hands-On Demo

[Compare Model Router Modes and Subsets](./demo-model-router.md) deploys four router deployments (Balanced, Quality, Cost and a subset), sends the same prompts to each, and compares which models served them and at what cost. Every step runs in Python from `model-router-py/` and in C# from `model-router-cs/`.

## Key Topics

- [Model router for Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/openai/concepts/model-router)
- [Use model router](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/model-router)
- [How model router works](https://learn.microsoft.com/en-us/azure/foundry/openai/concepts/model-router-how-it-works)
- [Monitor model router](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/monitor-model-router)
- [Evaluate model router for your workload](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/evaluate-model-router)
- [Use model router with Foundry agents](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/model-router-agents)
- [Deploy and use Claude models](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/use-foundry-models-claude)
