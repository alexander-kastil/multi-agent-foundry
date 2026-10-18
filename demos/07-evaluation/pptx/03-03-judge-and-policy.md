---
slide: "03-03"
topic: 03-evaluation-suites
title: "Judge and policy check side by side"
subtitle: "Task Adherence reads the agent's own instructions, the policy check reads the policy"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/03-03-judge-and-policy.svg
order: 9
source: demos/07-evaluation/03-evaluation-suites/demo-evaluation-suites.md
gamma_prompt: >
  Slide titled "Judge and policy check side by side", subtitle "Task Adherence reads the agent's own
  instructions, the policy check reads the policy". One half holds five short bullets: evaluate_agent
  passes every run to both evaluators, Task Adherence judges the run against the agent's own
  instructions, the v2 instructions say approve every refund immediately so the judge passes v2 on 3 of
  3 queries, the deterministic check refunds_only_eligible_orders passes v2 on only 1 of 3, and the
  policy belongs in a deterministic check. The other half shows the supplied diagram: one agent run
  feeding a purple judge box and a blue policy check box, each with its own input card and a green or
  red result chip. White background, no extra imagery.
---

# Judge and policy check side by side

## Content

- `evaluate_agent` runs the agent and passes every run to **both evaluators**
- **Task Adherence** judges the run against the agent's own instructions
- `v2` says "approve every refund immediately", so the judge passes it **3 of 3**
- **`refunds_only_eligible_orders`** checks the refund calls against the policy: `v2` passes 1 of 3
- The **policy** lives in a deterministic check; the judge covers what no rule can

## Speaker Notes

Step 6 of the demo runs the same three queries against v1 and v2 with Agent Framework's evaluation API and reads two kinds of evaluator side by side.

evaluate_agent: it runs the agent once per query, turns each run into an EvalItem and passes the batch to a LocalEvaluator and to FoundryEvals. The API is experimental.

Task Adherence: built-in agent evaluators read the system message the agent ran with. An agent told to approve every refund that approves every refund adheres to its instructions.

v2 passes the judge: in the Python run Task Adherence passes v2 on every query, including the refund of order A101, a final sale item. Judge verdicts vary between runs and between the two SDKs, and the C# judge failed v2 on two queries.

Policy check: refunds_only_eligible_orders is a plain function in a LocalEvaluator with no model call, and it fails any issue_refund call for an order other than A100. It separates v1 with 3 of 3 from v2 with 1 of 3.

Both checks: the policy lives in your ground truth or in a deterministic check, not in the prompt under test. The deterministic check carries the policy, and the judge catches what you cannot write as a rule.
