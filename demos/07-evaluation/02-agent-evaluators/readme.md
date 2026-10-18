# Agent Evaluators: Task Adherence & Tools

A final answer that reads well can hide a broken run: the agent refunded the wrong order, skipped a lookup, ignored an error from a tool or answered a question nobody asked. Generic quality scores such as fluency miss all of that. Agent evaluators score what the agent did, not only what it said, and they behave like unit tests: each one returns Pass or Fail with a reason.

## System Evaluation and Process Evaluation

Foundry groups its agent evaluators by the question they answer. System evaluation looks at the outcome of the whole run. Process evaluation checks the steps, which for an agent mostly means tool calls.

| Evaluator | Kind | Question it answers |
| --- | --- | --- |
| Task Completion (preview) | System | Did the agent deliver a usable result for the whole request? |
| Task Adherence (preview) | System | Did the agent follow its instructions, rules and required procedure? |
| Intent Resolution (preview) | System | Did the agent understand and address what the user wanted? |
| Task Navigation Efficiency | System | Did the agent take the expected steps, compared with a ground-truth path? |
| Customer Satisfaction (preview) | System | How satisfied would the user be across the conversation? |
| Tool Call Accuracy | Process | Were the right tool calls made with the right parameters? |
| Tool Selection | Process | Did the agent pick the necessary tools and no others? |
| Tool Input Accuracy | Process | Are all parameters grounded, correctly typed and complete? |
| Tool Output Utilization | Process | Did the agent use what the tools returned, without inventing details? |
| Tool Call Success | Process | Did the tool calls run without technical errors? |

Output Quality and Tool Use Quality (both preview) are composite evaluators that batch several of these into one judge call to cut cost; they want a GPT-5.6 family judge. Quality Grader is deprecated and can no longer run.

The failure modes are different, which is why you run several together. A run that calls the wrong tool but writes a confident answer can still pass Task Completion, while Tool Selection and Task Adherence fail it. A run with a perfect tool call that answers the wrong question passes Tool Call Accuracy and fails Intent Resolution.

## The Data an Agent Evaluator Reads

Agent evaluators need the trajectory, not only the final text. Foundry reads it from message arrays that carry system instructions, tool calls and tool results, plus the schemas of the tools the agent could use.

```json
{
  "query": [
    {"role": "system", "content": "You are a customer service agent. Look up an order before you act on it."},
    {"role": "user", "content": [{"type": "text", "text": "Order A100 arrived damaged. Please refund it."}]}
  ],
  "response": [
    {"role": "assistant", "content": [{"type": "tool_call", "tool_call_id": "c1", "name": "lookup_order", "arguments": {"order_id": "A100"}}]},
    {"role": "tool", "tool_call_id": "c1", "content": [{"type": "tool_result", "tool_result": {"status": "delivered", "total": 24.0}}]},
    {"role": "assistant", "content": [{"type": "text", "text": "I issued a refund of 24.00 euro for order A100."}]}
  ],
  "tool_definitions": [
    {"name": "lookup_order", "description": "Look up an order by id.", "parameters": {"type": "object", "properties": {"order_id": {"type": "string"}}, "required": ["order_id"]}}
  ]
}
```

A row can use `messages` instead of `query` and `response`. Each evaluator needs a different subset: Tool Call Success needs only `response`, Task Adherence needs `query` and `response`, and the tool evaluators add `tool_definitions`. The supported tool types are file search, function tools, MCP and knowledge-based MCP; for Azure AI Search, Bing grounding, SharePoint, code interpreter, Fabric and web search the docs advise against Tool Call Accuracy, Tool Input Accuracy, Tool Output Utilization and Tool Call Success.

## Running Evaluators in the Cloud

A cloud evaluation has three parts: a data source schema, testing criteria that bind evaluators to fields with `data_mapping`, and a run that supplies the data. Results are stored in the project and open in the portal under Evaluations.

```python
testing_criteria = [
    TestingCriterionAzureAIEvaluator(
        type="azure_ai_evaluator",
        name="task_adherence",
        evaluator_name="builtin.task_adherence",
        initialization_parameters={"deployment_name": "gpt-5.4"},
        data_mapping={"query": "{{item.query}}", "response": "{{item.response}}"},
    ),
]
evaluation = openai_client.evals.create(name="agent-evaluators", data_source_config=data_source_config, testing_criteria=testing_criteria)
run = openai_client.evals.runs.create(eval_id=evaluation.id, name="run-1", data_source=data_source)
```

When the run generates the answers itself, with an `azure_ai_agent` target, map the response from `{{sample.output_items}}` for evaluators that need the tool calls and from `{{sample.output_text}}` for plain text evaluators. Prompt agents run their function tools on the client, so an agent target cannot execute them; evaluate recorded trajectories instead.

## Reading a Result

Every evaluator returns a label, a reason and a threshold. Those on a 1 to 5 scale, such as Intent Resolution and Tool Call Accuracy, also return the score, and the threshold converts it to pass or fail.

```json
{
  "name": "Intent Resolution",
  "metric": "intent_resolution",
  "label": "pass",
  "score": 4,
  "reason": "Agent correctly identified the user's intent to book a flight to Paris",
  "threshold": 3,
  "passed": true
}
```

An evaluator that has nothing to judge, such as Tool Call Success on a run with no tool calls, is skipped and reports no pass or fail. Treat the judge like any instrument: it is a model, results move between runs, and a reason that you disagree with is a signal to refine the evaluator rather than to trust it.

## Running Evaluators Locally

The `azure-ai-evaluation` package runs the same judges in your own process. Public classes cover Intent Resolution, Task Adherence and Tool Call Accuracy; the other agent evaluators run in the cloud. Pass `is_reasoning_model=True` when the judge is a reasoning model such as `gpt-5.4`.

```python
from azure.ai.evaluation import TaskAdherenceEvaluator

task_adherence = TaskAdherenceEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)
result = task_adherence(query=item["query"], response=item["response"])
print(result["task_adherence_result"], result["task_adherence_reason"])
```

On .NET, the `Microsoft.Extensions.AI.Evaluation.Quality` package ships `IntentResolutionEvaluator`, `TaskAdherenceEvaluator` and `ToolCallAccuracyEvaluator` on top of any `IChatClient`. They return typed metrics and are marked experimental.

```csharp
var taskAdherence = new TaskAdherenceEvaluator();
EvaluationResult result = await taskAdherence.EvaluateAsync(
    query, response, new ChatConfiguration(judgeClient), [new TaskAdherenceEvaluatorContext(tools)]);
```

## Hands-On Demo

[Score Agent Runs With Task and Tool Evaluators](demo-agent-evaluators.md) builds a set of agent trajectories with known defects, scores them locally in Python and C# and in the cloud with all eight evaluators, and reads the result as a matrix of defect against evaluator.

## Key Topics

- [Agent evaluators](https://learn.microsoft.com/en-us/azure/foundry/concepts/evaluation-evaluators/agent-evaluators)
- [Evaluation dataset schema](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/evaluation-dataset-schema)
- [Run evaluations from the SDK](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation)
- [Evaluate model and agent targets](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation-targets)
- [The Microsoft.Extensions.AI.Evaluation libraries](https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries)
- [Agent Framework evaluation](https://learn.microsoft.com/en-us/agent-framework/agents/evaluation)
