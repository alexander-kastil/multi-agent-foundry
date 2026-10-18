# Compare Model Router Modes and Subsets

Picking one model for every request means paying frontier prices for trivial questions or accepting weak answers on hard ones. Model router removes that choice from your code, but only if you know how its modes and subsets behave on your own prompts. In this demo you deploy four router deployments, send identical prompts to each and read which models actually served them.

What you practice here: deploying a model router with a routing mode and a model subset, and reading the serving model from every response.

Duration: about 50 minutes.

## Prerequisites

- Azure CLI signed in, Python 3.10 or later or the .NET 10 SDK, and Contributor, Foundry Account Owner or Foundry Owner on the Foundry resource (creating deployments is a control plane action that Foundry User and Foundry Project Manager do not include).
- Quota for four deployments of 10 capacity units each (10,000 tokens per minute each) in a region that supports model router. Sweden Central does.
- Deployments named `gpt-5-mini` and `gpt-5.4` for the subset.

## Run it

`provision.azcli` and `deprovision.azcli` sit in the topic folder and own the deployment lifecycle. The Python project in `model-router-py/` and the C# project in `model-router-cs/` only call the deployments.

```bash
cd demos/01-foundry-essentials/02-model-router
bash provision.azcli

cd model-router-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
```

Set the endpoint of your project in `.env`. `MODEL_ROUTER_DEPLOYMENT_NAME` selects the deployment used by the single-call and multi-turn scripts.

```text
PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents
MODEL_DEPLOYMENT_NAME=gpt-5.4
MODEL_ROUTER_DEPLOYMENT_NAME=m1-router-balanced
```

The C# commands in the steps run from this same folder with `--project ../model-router-cs`, and .NET reads the `.env` of the folder you run from. Restore the C# project once:

```bash
dotnet restore ../model-router-cs
```

## Step 1: Deploy four routers with different routing settings

A router is deployed like any other model: `format`, `name` and `version` identify the model, and an optional `routing` block overrides the default Balanced mode or restricts the pool. `provision.azcli` sends one PUT per deployment.

```bash
deploy_router m1-router-quality '{
  "sku": {"name": "GlobalStandard", "capacity": 10},
  "properties": {
    "model": {"format": "OpenAI", "name": "model-router", "version": "2025-11-18"},
    "routing": {"mode": "quality"}
  }
}'
```

The other three deployments follow the same pattern: no `routing` block for `m1-router-balanced`, mode `cost` for `m1-router-cost`, and mode `balanced` with a two-model `models` array for `m1-router-subset`.

Expected output (trimmed):

```json
{
  "name": "m1-router-balanced",
  "routing": null,
  "state": "Succeeded"
}
{
  "name": "m1-router-quality",
  "routing": {
    "mode": "quality"
  },
  "state": "Succeeded"
}
...
{
  "name": "m1-router-subset",
  "routing": {
    "mode": "balanced",
    "models": [
      { "format": "OpenAI", "name": "gpt-5-mini", "version": "2025-08-07" },
      { "format": "OpenAI", "name": "gpt-5.4", "version": "2026-03-05" }
    ]
  },
  "state": "Succeeded"
}
```

## Step 2: See the routers as the project sees them

Deployments created on the resource are visible to the project. `list_deployments.py` filters the project's deployments to the model router.

```python
for deployment in project.deployments.list():
    if deployment.model_name == "model-router":
        print(f"{deployment.name:<22} {deployment.model_name} {deployment.model_version} {deployment.sku.name} capacity={deployment.sku.capacity}")
```

```csharp
foreach (ModelDeployment deployment in project.Deployments.GetDeployments().OfType<ModelDeployment>())
{
    if (deployment.ModelName == "model-router")
    {
        Console.WriteLine($"{deployment.Name,-22} {deployment.ModelName} {deployment.ModelVersion} {deployment.Sku.Name} capacity={deployment.Sku.Capacity}");
    }
}
```

```bash
.venv/Scripts/python list_deployments.py
dotnet run --project ../model-router-cs -- list
```

Expected output:

```text
m1-router-balanced     model-router 2025-11-18 GlobalStandard capacity=10
m1-router-quality      model-router 2025-11-18 GlobalStandard capacity=10
m1-router-cost         model-router 2025-11-18 GlobalStandard capacity=10
m1-router-subset       model-router 2025-11-18 GlobalStandard capacity=10
```

All four report the same model and version. The routing mode and subset are deployment settings, so the model list does not show them.

## Step 3: Send one call and read the serving model

The router is called exactly like a normal model: the deployment name goes into `model`. What differs is `response.model`, which names the model that did the work.

```python
response = openai.responses.create(
    model=os.environ["MODEL_ROUTER_DEPLOYMENT_NAME"],
    input="In one sentence, name the most popular tourist destination in Seattle.",
)

print(f"served by   : {response.model}")
```

```csharp
ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(deployment);
ResponseResult response = await responses.CreateResponseAsync("In one sentence, name the most popular tourist destination in Seattle.");

Console.WriteLine($"served by   : {response.Model}");
```

```bash
.venv/Scripts/python route_once.py
dotnet run --project ../model-router-cs -- route
```

Expected output (the serving model varies between runs):

```text
deployment  : m1-router-balanced
served by   : grok-4-1-fast-reasoning
answer      : The most popular tourist destination in Seattle is Pike Place Market.
```

Your own run can name a different model, and that is the feature working. The router chose from its pool, not from your `gpt-5.4` deployment.

## Step 4: Compare Balanced, Quality and Cost on the same prompts

`compare.py` sends three prompts of rising difficulty (a lookup, a coding task and a reasoning task) to every deployment named on the command line and prints the serving model, latency and token count.

```python
PROMPTS = {
    "lookup": "What is the capital of Austria?",
    "code": "Write a Python function that merges overlapping intervals and explain its time complexity.",
    "reasoning": "A bat and a ball cost 1.10 in total and the bat costs 1.00 more than the ball. Solve it with algebra and explain why intuition fails.",
}
```

```csharp
ResponseResult response = await responses.CreateResponseAsync(prompt);
Console.WriteLine($"{deployment,-22} {label,-10} {response.Model,-32} {elapsed,5:F1} {response.Usage.TotalTokenCount,7}");
```

```bash
.venv/Scripts/python compare.py m1-router-balanced m1-router-quality m1-router-cost
dotnet run --project ../model-router-cs -- compare m1-router-balanced m1-router-quality m1-router-cost
```

Expected output (model names, latency and tokens vary):

```text
deployment             prompt     served by                          sec  tokens
m1-router-balanced     lookup     gpt-6-luna-2026-09-22              3.1      61
m1-router-balanced     code       gpt-5.6-sol-2026-07-09             3.8     399
m1-router-balanced     reasoning  grok-4-1-fast-reasoning            5.1     806
m1-router-quality      lookup     gpt-6-sol-2026-09-22               1.4      29
m1-router-quality      code       gpt-6-astra-2026-09-03             6.6     341
m1-router-quality      reasoning  grok-4-1-fast-reasoning            5.7     746
m1-router-cost         lookup     gpt-6-luna-2026-09-22              1.5      65
m1-router-cost         code       gpt-6-luna-2026-09-22              2.7     396
m1-router-cost         reasoning  gpt-5-nano-2025-08-07             15.0    6246
```

Read the table by column. In this run Quality moved the lookup and the coding task to the larger `gpt-6-sol` and `gpt-6-astra`, Cost kept all three on the smaller `gpt-6-luna` and `gpt-5-nano`, and the cheapest choice for the reasoning prompt spent about eight times the tokens of the others. Cheaper per token is not always cheaper per task, so evaluate the modes on your own prompts before you commit.

## Step 5: Constrain the router with a model subset

The subset deployment may only choose between `gpt-5-mini` and `gpt-5.4`. Run the same prompts against it.

```bash
.venv/Scripts/python compare.py m1-router-subset
dotnet run --project ../model-router-cs -- compare m1-router-subset
```

Expected output:

```text
deployment             prompt     served by                          sec  tokens
m1-router-subset       lookup     gpt-5-mini-2025-08-07              3.2     171
m1-router-subset       code       gpt-5-mini-2025-08-07              8.6    1783
m1-router-subset       reasoning  gpt-5-mini-2025-08-07              10.3    2178
```

Every call stayed inside the two approved models, which is the guarantee a subset gives you. The router picked `gpt-5-mini` for all three prompts, and `gpt-5.4` stayed available as the failover target.

## Step 6: Watch the router per turn in a conversation

With Chat Completions you send the whole history on every turn, and the router decides again each time. `multi_turn.py` keeps a message list and prints the serving model after every turn.

```python
for turn in turns:
    messages.append({"role": "user", "content": turn})
    response = openai.chat.completions.create(
        model=os.environ["MODEL_ROUTER_DEPLOYMENT_NAME"],
        messages=messages,
    )
    answer = response.choices[0].message.content
    messages.append({"role": "assistant", "content": answer})
    print(f"served by : {response.model}")
```

```csharp
ChatClient chat = project.ProjectOpenAIClient.GetChatClient(Environment.GetEnvironmentVariable("MODEL_ROUTER_DEPLOYMENT_NAME")!);

messages.Add(new UserChatMessage(turn));
ChatCompletion completion = await chat.CompleteChatAsync(messages);
messages.Add(new AssistantChatMessage(completion.Content[0].Text));
Console.WriteLine($"served by : {completion.Model}");
```

```bash
.venv/Scripts/python multi_turn.py
dotnet run --project ../model-router-cs -- multi-turn
```

Expected output (trimmed):

```text
user      : Plan a two-day food trip to Vienna.
served by : gpt-5.6-terra-2026-07-09
answer    : ## Two-Day Food Trip to Vienna  ### Day 1 ...

user      : Swap day two for a day trip to Salzburg.
served by : gpt-5.6-terra-2026-07-09
answer    : ## Day 2 ...

user      : List the packing essentials as five bullet points.
served by : grok-4.6
answer    : - Comfortable walking shoes for cobblestones in Vienna and Salzburg - Light layers ...
```

The last turn switched models in the middle of one conversation. That is safe because the full history travels with every request, and it is why session affinity exists for workloads that depend on prompt caching.

## Verify

You have verified the demo, in Python or in C#, when the comparison shows at least two different serving models across the Balanced, Quality and Cost deployments, and every row for `m1-router-subset` names `gpt-5-mini` or `gpt-5.4`. The exact models differ from run to run because the pool and the routing decisions change.

## Cleanup

Delete the four deployments. The script removes only the `m1-router-*` names.

```bash
cd ..
bash deprovision.azcli
```

The C# `bin/` and `obj/` folders are git-ignored build output.
