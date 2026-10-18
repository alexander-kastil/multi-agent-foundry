# Evaluation Suites: Judges & Baselines

One evaluation run tells you how an agent did today. A release decision needs more: a fixed set of cases that covers the situations you care about, a judge you have measured against humans, and a stored baseline to compare the next version with. Together they turn "it looks fine" into a gate that passes or blocks a change.

## A Suite Is Designed, Not Collected

A useful suite covers combinations, not just happy paths. Generate cases from a grid of dimensions so that coverage is a decision: interaction category times customer persona times adversarial condition gives 3 x 2 x 3 = 18 combinations, and a model writes one case for each. Every case carries a `ground_truth` that states what a correct agent must do.

```json
{
  "id": "case-001",
  "interaction_type": "refund_calculation",
  "persona": "first-time buyer",
  "adversarial": "vague request",
  "canary": false,
  "customer_request": "My order was delivered 12 days ago. I paid 27 for a box listed at 36. How much do I get back?",
  "ground_truth": {
    "expected_action": "Refund the amount actually paid, not the list price.",
    "must_include": ["Refund amount for the item is 27, not 36"],
    "should_refuse": false
  }
}
```

Two kinds of case are added on purpose. Canaries are the easy cases with no adversarial condition; if they fail, the full suite is not worth running. Historical probes are cases taken from past incidents, appended to every run so a fixed bug stays fixed.

![The 18 cases from 3 interaction categories, 2 personas and 3 adversarial conditions, with the 6 canaries marked, 3 historical probes beside them and the ground truth of case-001](../pptx/images/03-01-suite-from-grid.svg)

## A Judge Is an Instrument

An LLM judge scores a response against the rubric and the ground truth. Before it gates anything, measure it: label a small set of responses by hand, run the judge on the same set and compute the agreement of the pass and fail decisions with Cohen's kappa. A kappa above 0.7 is a common bar for "reliable for gating".

```python
human = [item["human_score"] >= PASS_SCORE for item in examples]
automatic = [score >= PASS_SCORE for score in scores]
kappa = cohen_kappa(human, automatic)
```

Agreement with humans does not make one judge infallible. Score the same cases with several judge models, take the mean and send a case to a person when the spread between judges exceeds a few points. Keep the rubric on the ground truth and ignore tone, otherwise a friendly wrong answer outscores a curt correct one.

## Baselines and Regression Gates

A baseline is the stored result of the version you trust: task completion rate, average score and the completion rate per interaction type. A candidate version runs the same suite and the gate compares the two under explicit limits.

```json
{
  "task_completion_drop": 0.1,
  "goal_achievement_drop": 0.5,
  "per_type_drop": 0.25,
  "canary_min_rate": 0.8
}
```

The per-type limit matters because an average can hide a regression in one category. The limits also have to absorb noise: both the agent and the judge are models, so two runs of the same version differ by a few points. Set a limit from the spread you observe on repeated runs of one version, not from the spread you wish for.

The gate runs in stages: canaries first, then the full suite plus probes, then the comparison. A failed canary stage stops the pipeline before the expensive stage.

![The regression gate in stages: canaries, full suite and baseline comparison, with a BLOCK exit after the canaries and after the comparison and APPROVE DEPLOYMENT at the end](../pptx/images/03-02-staged-regression-gate.svg)

## Judges Grade Against the Agent's Own Instructions

Built-in agent evaluators such as Task Adherence read the system message the agent ran with. An agent told to approve every refund and that approves every refund adheres to its instructions, so a judge can pass a prompt that violates your policy.

The policy lives in your ground truth or in a deterministic check, not in the prompt under test. Run both kinds of check and read them side by side.

![One v2 run read by two checks: the Task Adherence judge reads the agent's own instructions and passes 3 of 3, the refunds_only_eligible_orders check reads the returns policy and passes 1 of 3](../pptx/images/03-03-judge-and-policy.svg)

## Agent Framework Evaluation

Agent Framework has an evaluation API built around three types: `EvalItem` for one interaction, `Evaluator` for a scorer and `EvalResults` for the outcome. `evaluate_agent` runs the agent once per query, turns each run into an `EvalItem` and passes the batch to every evaluator you list. The API is experimental.

```python
from agent_framework import ExpectedToolCall, LocalEvaluator, evaluate_agent, evaluator, tool_called_check
from agent_framework.foundry import FoundryEvals

@evaluator
def refunds_only_eligible_orders(conversation: list) -> bool:
    ...

results = await evaluate_agent(
    agent=agent,
    queries=QUERIES,
    expected_tool_calls=[[ExpectedToolCall("lookup_order", {"order_id": "A100"})]],
    evaluators=[
        LocalEvaluator(tool_called_check("lookup_order"), refunds_only_eligible_orders),
        FoundryEvals(client=client, evaluators=[FoundryEvals.TASK_ADHERENCE, FoundryEvals.TOOL_CALL_ACCURACY]),
    ],
)
```

`LocalEvaluator` runs checks in your process with no model call: built-in checks such as `keyword_check` and `tool_called_check`, and functions decorated with `@evaluator` whose parameter names (`query`, `response`, `conversation`, `tools`, `expected_output`) select the data they receive. `FoundryEvals` sends the items to the Foundry evaluation service and returns a report URL for the portal. Each evaluator yields its own `EvalResults`; `num_repetitions` repeats each query to expose non-determinism and `conversation_split` chooses what counts as the response.

## Hands-On Demo

[Gate a Release With a Judged Evaluation Suite](demo-evaluation-suites.md) calibrates a judge against human labels, saves a baseline, blocks a regressed prompt at the canary stage, approves a good one and then runs the same comparison with Agent Framework's `evaluate_agent`. The finished projects are `evaluation-suites-py/` and `evaluation-suites-af-py/` (Python) and `evaluation-suites-cs/` (C#, `dotnet run -- generate | calibrate | gate | agent-eval`).

## Key Topics

- [Run evaluations from the SDK](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation)
- [Evaluate existing datasets](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation-datasets)
- [Generate a synthetic evaluation dataset](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/evaluation-dataset-synthetic)
- [Custom evaluators](https://learn.microsoft.com/en-us/azure/foundry/concepts/evaluation-evaluators/custom-evaluators)
- [Agent Framework evaluation](https://learn.microsoft.com/en-us/agent-framework/agents/evaluation)
- [Microsoft Foundry evaluation in Agent Framework](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/evaluation/microsoft-foundry)
