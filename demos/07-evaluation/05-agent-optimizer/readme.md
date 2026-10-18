# Agent Optimizer: Instructions & Tool Tuning

Tuning an agent by hand is a loop of guesses: write instructions, try a few inputs, change a sentence, try again. It is slow, it depends on who is typing and nobody can say whether version 7 is better than version 4.

The Foundry agent optimizer replaces the guessing with measurement. It scores your agent on a dataset, proposes alternative configurations, scores those on the same dataset and ranks them.

The agent optimizer is in preview, and the quickstart lists an access requirement for the subscription; if job creation is rejected, check that first.

## What the Optimizer Changes

The optimizer improves the configuration of an agent, not its code. What it can change depends on the agent type.

| Target | What improves | Prompt agent | Hosted agent |
| --- | --- | --- | --- |
| Instructions | Rewrites the system prompt to score higher | Yes | Yes |
| Tool descriptions | Rewrites function and parameter descriptions; names, types and required fields stay | Yes, function tools | Yes, function tools |
| Skills | Refines the body of each reusable skill | No | Yes |
| Model | Compares candidate deployments on quality against cost | Yes | Yes |

A prompt agent's function tools run on the client, so the optimizer improves how the model chooses and fills them but never executes them. A hosted agent's tools do run during evaluation, so point them at test endpoints or mocks.

## The Loop It Automates

Every run follows the same six steps:

1. Select the baseline, which for a prompt agent is a version.
2. Select an evaluation dataset of representative tasks.
3. Select evaluators that measure what you want.
4. Run the optimizer, which evaluates the baseline, generates candidates and evaluates them on the same data.
5. Review the ranked results.
6. Apply the winner.

Two models are involved. The eval model scores responses and can be any chat model. The optimization model generates the candidates and must be from the supported list: `gpt-5`, `gpt-5.1`, `gpt-5.2`, `gpt-5.4`, `gpt-5.5` or the DeepSeek models.

A stronger optimization model usually proposes better candidates because it reasons over all the failures at once.

## Starting a Job From Python

The SDK exposes the job as `AgentOptimizationJob`. For a prompt agent the target is the agent name and version, the dataset can be supplied inline as test cases with a `query` and an optional `ground_truth`, and the evaluators are referenced by name.

```python
job = AgentOptimizationJob(
    target_configuration=AgentOptimizationFoundryAgentTargetConfiguration(name="my-agent", version="1"),
    optimization_model_configuration=AgentOptimizationModelConfiguration(model="gpt-5.4"),
    optimization_configuration=AgentOptimizationConfiguration(
        evaluation_configuration=AgentOptimizationEvaluationConfiguration(
            training_set=AgentOptimizationTargetCompletionEvaluationSet(
                source=AgentOptimizationTargetCompletionInlineDataSource(test_cases=test_cases)
            ),
            evaluators=[AgentOptimizationEvaluator(name="builtin.f1_score")],
            evaluation_model=EvaluationModelConfiguration(model="gpt-5-mini"),
        ),
        candidate_search_configuration=AgentOptimizationCandidateSearchConfiguration(max_candidates=3),
        agent_optimization_space=AgentOptimizationSpace(target_attributes=[TargetAttribute.INSTRUCTIONS]),
    ),
)
poller = project_client.agents.begin_create_optimization_job(job=job, polling=False)
job_id = poller.details.get("job_id")
```

Inline data supports up to 500 test cases, or 2,000 when you supply a separate validation set. Registered datasets and datasets generated from agent traces work as well.

The evaluators must find their input columns in the dataset by name, because the service does no column mapping; a job whose dataset lacks a column an evaluator needs is rejected before it starts. The SDK also offers `estimate_optimization_job` for a cost range and `promote_optimization_candidate` to turn a candidate into a new agent version.

## Reading a Result

The optimizer scores each candidate between 0.0 and 1.0: it rescales every evaluator's raw score to that range, flips evaluators where lower is better and averages across tasks. The best candidate is marked, and the baseline is always in the list.

| Improvement over baseline | Reading |
| --- | --- |
| Less than 0.03 | Noise |
| 0.03 to 0.10 | Moderate; worth deploying |
| 0.10 to 0.20 | Significant |
| More than 0.20 | Major, usually from a poor baseline |

Look beyond the score. Optimized instructions are often longer, so input tokens and cost rise with them, and you decide whether the increase is proportional to the gain. Results also vary between runs of the same job, because the candidate generation is itself a model; repeat the job or raise `max_candidates` before you trust a single outcome.

Promotion creates a new prompt agent version with the candidate's configuration. If the agent is pinned to a version, the new version receives no traffic until you set it as the active version. Always confirm the win with an evaluation of your own on the promoted version, ideally on data the optimizer did not see.

## The Dataset Is the Specification

The optimizer can only reward what the dataset and evaluators measure. A dataset of queries with no ground truth teaches it nothing about the answer you want; a metric that rewards the wrong behavior is optimized just as eagerly. For tool tuning the built-in tool evaluators skip a response that is only a function call, so a prompt agent's tool descriptions need an evaluator that reads the tool call itself, such as a code-based custom evaluator that compares the chosen tool and its arguments with the expected ones.

## Prompt Optimizer in the Portal

Prompt Optimizer is a lighter tool for the same job. In the agent configuration panel in the Foundry portal, the pencil with sparkle icon next to Instructions opens a dialog that rewrites your instructions with prompt-engineering practice, shows the reason for each changed paragraph and lets you add suggestions such as "add guardrails for off-topic questions" and optimize again. It needs no dataset and no evaluation run.

The result lives only in the dialog until you select Use prompt, it handles text instructions only and it is available in selected regions. Because it applies general practice rather than your data, follow it with an evaluation. Use Prompt Optimizer to get a sound first draft, and the agent optimizer to tune that draft against measured behavior.

## Hands-On Demo

[Let the Optimizer Rewrite an Agent's Instructions](demo-agent-optimizer.md) scores a vague ticket-triage agent, runs an optimization job on twenty labeled tickets, compares the candidates, promotes the best one and confirms the gain independently, then tunes the tool descriptions of a second agent with a code-based evaluator. The Python project is `agent-optimizer-py/`; the C# project `agent-optimizer-cs/` runs the same steps against the REST API with `HttpClient`.

## Key Topics

- [What is the agent optimizer?](https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/agent-optimizer-overview)
- [Quickstart: Optimize a prompt agent](https://learn.microsoft.com/en-us/azure/foundry/agents/quickstarts/quickstart-optimize-prompt-agent)
- [Quickstart: Optimize a hosted agent](https://learn.microsoft.com/en-us/azure/foundry/agents/quickstarts/quickstart-optimize-hosted-agent)
- [Optimize agent prompts by using Prompt Optimizer](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/prompt-optimizer)
- [Custom evaluators](https://learn.microsoft.com/en-us/azure/foundry/concepts/evaluation-evaluators/custom-evaluators)
- [Run evaluations from the SDK](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation)
