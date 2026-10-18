# Sequential, Concurrent & Handoff Orchestration

Most multi-agent designs are one of a few shapes: a pipeline where each agent refines the last, a panel where several agents answer the same question in parallel, or a front desk that routes the customer to a specialist. Agent Framework ships each shape as a builder, so you pass in agents and get back a ready workflow instead of wiring executors and edges by hand.

![SequentialBuilder chains writer, reviewer and polisher, ConcurrentBuilder runs finance, security and compliance in parallel into an aggregator, HandoffBuilder links triage_agent with billing_agent and technical_agent](../pptx/images/03-01-orchestration-builders.svg)

The builders live in `agent_framework.orchestrations` (package `agent-framework-orchestrations`) and take their configuration as constructor parameters. What you get back is an ordinary `Workflow`, so everything from the previous topics applies: streaming events, outputs, checkpoints and human requests.

| Pattern | Builder | Control flow | Use it when |
| --- | --- | --- | --- |
| Sequential | `SequentialBuilder` | Fixed order, each agent sees the earlier conversation | Draft, review, polish pipelines |
| Concurrent | `ConcurrentBuilder` | All agents run in parallel, results are aggregated | Independent expert opinions on one input |
| Handoff | `HandoffBuilder` | Agents decide who owns the conversation next | Triage and specialist routing with a person in the loop |

## Sequential: A Pipeline of Agents

`SequentialBuilder(participants=[...])` runs the agents in list order. By default each agent receives the previous agent's whole conversation, both the input and the response. Set `chain_only_agent_responses=True` when each step should see only the previous reply, for example in translation chains.

```python
workflow = SequentialBuilder(
    name="announcement_pipeline",
    participants=[writer, reviewer, polisher],
    intermediate_output_from=[writer, reviewer],
).build()

result = await workflow.run(BRIEF)
print(result.get_outputs()[0].text)
```

The terminal output is an `AgentResponse` from the last participant. `intermediate_output_from` surfaces the earlier agents as `intermediate` events that you read from the run result. A custom `Executor` can sit in the list too; as the last participant it must call `ctx.yield_output(AgentResponse(...))`.

## Concurrent: A Panel of Experts

`ConcurrentBuilder(participants=[...])` sends the same input to every participant at once and waits for all of them. The default aggregator produces one `AgentResponse` with one assistant message per participant. A custom aggregator replaces that step with your own reduction.

```python
async def summarize(results: list[AgentExecutorResponse]) -> str:
    opinions = "\n".join(f"{r.executor_id}: {r.agent_response.text}" for r in results)
    return (await summarizer.run(opinions)).text


workflow = ConcurrentBuilder(name="proposal_review", participants=participants).with_aggregator(summarize).build()
```

The aggregator can be sync or async and receives a `list[AgentExecutorResponse]`. Wall-clock time is the slowest participant, not the sum.

## Handoff: Agents Pass the Conversation

`HandoffBuilder` builds a mesh with no central manager. The start agent talks to the user and can transfer the conversation to a specialist by calling a handoff tool that the builder registers for it. The receiving agent takes ownership of the task and sees the earlier user and agent messages.

```python
workflow = (
    HandoffBuilder(name="support_handoff", participants=[triage, billing, technical])
    .with_start_agent(triage)
    .add_handoff(triage, [billing, technical])
    .add_handoff(billing, [triage])
    .add_handoff(technical, [triage])
    .build()
)
```

Every participant must be created with `require_per_service_call_history_persistence=True`, otherwise `build()` raises a `ValueError` that lists the agents missing the flag. Handoff supports only `Agent` participants and agents that run tools locally.

When an agent answers without handing off, the workflow pauses with a `request_info` event whose data is a `HandoffAgentUserRequest`. Answer with `HandoffAgentUserRequest.create_response(text)` or end the conversation with `HandoffAgentUserRequest.terminate()`. A tool declared with `@tool(approval_mode="always_require")` pauses the same way with a function approval request, which you answer with `request.data.to_function_approval_response(approved=True)`.

![Sequence of the support handoff: triage_agent hands off to billing_agent, issue_refund pauses for a tool approval, and two HandoffAgentUserRequest pauses wait for the user](../pptx/images/03-02-handoff-tool-approval.svg)

```python
async for event in workflow.run(responses=responses, stream=True):
    if event.type == "handoff_sent":
        print(f"{event.data.source} -> {event.data.target}")
```

`with_autonomous_mode(turn_limits={...})` lets agents keep going without a user reply up to a turn limit, and `termination_condition=` ends the conversation on a message predicate. Both are covered again in the task decomposition topic.

## Choosing a Pattern

Handoff differs from agent-as-tools: with a handoff the receiving agent owns the rest of the task, while a tool call returns control to the caller. If you need a manager that plans and tracks progress, move on to group chat and Magentic in the next topic. If the steps and branches are known up front, a hand-built `WorkflowBuilder` graph is simpler and cheaper than any agent-driven routing.

## The Same Patterns in .NET

`AgentWorkflowBuilder` provides `BuildSequential`, `BuildConcurrent` and `CreateHandoffBuilderWith`. Agents come from `AIProjectClient.AsAIAgent(model:, instructions:, name:, description:)`, which lives in the preview package `Microsoft.Agents.AI.Foundry` and pulls the matching preview of `Azure.AI.Projects`.

```csharp
var sequential = AgentWorkflowBuilder.BuildSequential([writer, reviewer, polisher]);

var handoff = AgentWorkflowBuilder.CreateHandoffBuilderWith(triage)
    .WithHandoffs(triage, [billing, technical])
    .WithHandoffs([billing, technical], triage)
    .Build();

await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
```

The final `WorkflowOutputEvent` carries a `List<ChatMessage>`. Streaming updates are reported as `AgentResponseUpdateEvent`, which also matches a `WorkflowOutputEvent` pattern, so test for the update type first when you only want the final transcript.

## Hands-On Demo

[Run Three Agent Patterns with One Builder Each](demo-orchestration-patterns.md) builds a sequential announcement pipeline, a concurrent proposal review with a custom aggregator and a support handoff with a tool approval, then runs the same three in C#.

## Key Topics

- [Sequential orchestration](https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/sequential)
- [Concurrent orchestration](https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/concurrent)
- [Handoff orchestration](https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/handoff)
- [Human-in-the-loop with orchestrations](https://learn.microsoft.com/en-us/agent-framework/workflows/human-in-the-loop)
- [Agents in workflows](https://learn.microsoft.com/en-us/agent-framework/workflows/agents-in-workflows)
