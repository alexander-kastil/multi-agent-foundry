# Guardrails: Prompt Shields & Intervention Points

A guardrail is a named set of controls that Microsoft Foundry applies to a model or an agent at runtime. Each control names a risk to detect, the intervention point where to look for it, and the action to take. Agent guardrails are in preview, and this topic shows how to read them, create one, attach it to an agent and watch each intervention point work.

## Controls and intervention points

A control combines three things: the risk, such as violence or a prompt attack; the intervention point, which is where in the agent's run the content is scanned; and the action. Four intervention points exist.

| Intervention point | What is scanned | Applies to |
| --- | --- | --- |
| User input | The prompt sent to a model or agent | Models and agents |
| Tool call (preview) | The action and data the agent proposes to send to a tool | Agents only |
| Tool response (preview) | The content a tool returns to the agent, before it is added to memory or shown to the user | Agents only |
| Output | The final completion returned to the user | Models and agents |

For agents, the action is annotate and block; annotate-only is available for models.

Each intervention point adds roughly 50 to 100 ms of latency. Tool call and tool response controls take effect only for tools that support moderation, such as Azure AI Search, Azure Functions, OpenAPI, Bing Grounding and Browser Automation; the intervention points article has the full list.

## Prompt Shields

Prompt Shields detect two kinds of attacks. A user prompt attack tries to bypass system instructions or safety training, for example by replacing the assistant's persona or by encoding instructions. A document attack hides instructions in content the agent processes, such as a document, an email or a web page.

| Attack | Scanned at | Default |
| --- | --- | --- |
| User prompt attack | User input | On |
| Document attack | User input for models; also tool response for supported tools on agents (preview) | Off |

In the Responses API each control reports `detected` and `filtered`. A detected attack that was not blocked has `detected: true` and `filtered: false`. Spotlighting, a related preview feature that marks documents as lower trust, works only for models used through Chat Completions and does not apply to agents.

## Task adherence (preview)

Task adherence compares the tool call an agent plans with what the user asked for. A request to view calendar events paired with a planned `clear_calendar_events` call is a mismatch. The check is a REST call to Azure AI Content Safety that returns a risk flag and a reason, which your code can use to block the call or ask a person.

```json
{
  "taskRiskDetected": true,
  "details": "Planned action deletes calendar events, but user only requested to view them."
}
```

The API version is `2024-12-15-preview`, and the maximum input is 100,000 characters.

## Guardrails as a policy resource

In Azure Resource Manager a guardrail is a RAI policy on the Foundry resource. Its `contentFilters` list holds one entry per control, and the `source` field names the intervention point.

```json
{
  "properties": {
    "basePolicyName": "Microsoft.DefaultV2",
    "mode": "Blocking",
    "contentFilters": [
      { "name": "Violence", "source": "Prompt", "enabled": true, "blocking": true, "severityThreshold": "Low" },
      { "name": "Jailbreak", "source": "Prompt", "enabled": true, "blocking": true },
      { "name": "Indirect Attack", "source": "PostToolCall", "enabled": true, "blocking": true }
    ]
  }
}
```

`source` accepts `Prompt`, `Completion`, `PreToolCall`, `PostToolCall`, `PreRun` and `PostRun`. In this topic `Prompt` is user input, `Completion` is output, `PreToolCall` is tool call and `PostToolCall` is tool response.

`Jailbreak` is the API name for the user prompt attack control. A severity threshold of `Low` filters from the lowest severity up; the portal shows the same setting as blocking level High.

## Attach a guardrail to an agent

An agent uses the guardrail you assign. Without one, it inherits the guardrail of its model deployment, which is `Microsoft.DefaultV2` unless you changed it. The agent's guardrail fully overrides the model's, so a control missing from the agent's guardrail does not run just because the model's guardrail has it.

```python
agent = project.agents.create_version(
    agent_name="m8-guarded-agent",
    definition=PromptAgentDefinition(
        model=os.getenv("MODEL_DEPLOYMENT_NAME"),
        instructions=INSTRUCTIONS,
        rai_config=RaiConfig(rai_policy_name=os.getenv("RAI_POLICY_ID")),
    ),
)
```

`rai_policy_name` takes the full Azure Resource Manager ID of the policy, not its bare name. A policy that does not exist is rejected at creation for a prompt agent; Microsoft warns that hosted agents can fail open, so verify the policy exists and test it.

## What a block looks like

When user input is blocked, the call fails with HTTP 400 and the code `content_filter`, and the error body names the intervention point and the risk. When a tool response is blocked, the call succeeds with `status` set to `incomplete`, `incomplete_details.reason` set to `content_filter`, and a `content_filters` entry with `source_type` `post_tool` and `blocked` true.

```python
except BadRequestError as error:
    if error.code == "content_filter":
        print(error.body["content_filters"][0]["source_type"])
```

Read both signals in your client. A 200 response is not proof that the content passed.

## Hands-On Demo

[Block Prompt Attacks at Every Intervention Point](demo-guardrails.md) creates a guardrail, attaches it to an agent, and triggers the user input and tool response intervention points. It also calls Prompt Shields and Task Adherence directly. The finished projects are `guardrails-py/` (Python) and `guardrails-cs/` (C#, `dotnet run -- guarded | tool | shields | adherence`).

## Key Topics

- [Guardrails and controls overview](https://learn.microsoft.com/azure/foundry/guardrails/guardrails-overview)
- [Intervention points](https://learn.microsoft.com/azure/foundry/guardrails/intervention-points)
- [Prompt Shields in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/openai/concepts/content-filter-prompt-shields)
- [Task Adherence (preview)](https://learn.microsoft.com/azure/foundry/guardrails/task-adherence)
- [Configure guardrails and controls](https://learn.microsoft.com/azure/foundry/guardrails/how-to-create-guardrails)
- [Add guardrails to a hosted agent](https://learn.microsoft.com/azure/foundry/agents/how-to/add-hosted-agent-guardrails)
