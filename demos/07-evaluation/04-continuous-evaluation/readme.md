# Continuous Evaluation & AI Red Teaming

A release evaluation proves that a version was good on the day you shipped it. Production traffic then drifts: users ask new things, a tool changes, a model is updated, and an attacker tries your agent with inputs your test set never had. Continuous evaluation scores a sample of live traffic as it happens, and red teaming attacks the agent on purpose before someone else does.

## Three Ways to Evaluate After Release

Foundry offers three recurring evaluation modes. They share the same evaluators and the same `evals` API as the offline suite, so a rubric you trust before release also runs after it.

| Mode | What triggers it | What it scores |
| --- | --- | --- |
| Continuous evaluation (preview) | Every completed agent response, up to an hourly cap | A sample of live responses |
| Scheduled evaluation (preview) | A recurrence such as daily at 09:00 | A golden dataset or recent traces |
| Scheduled red teaming (preview) | A recurrence | Synthetic adversarial prompts |

Results appear on the Monitor tab of the agent in the Foundry portal, next to operational metrics: token usage, latency and run success rate. The dashboard also holds red teaming results and alert settings. All of it is stored in the Application Insights resource connected to the project.

## Continuous Evaluation Rules

A continuous evaluation is an evaluation object plus a rule. The evaluation lists the evaluators; the rule says when to run it. The data source is `azure_ai_source` with the `responses` scenario, so the service reads the agent response itself and no dataset is needed.

```python
evaluation = openai_client.evals.create(
    name="continuous-evaluation",
    data_source_config={"type": "azure_ai_source", "scenario": "responses"},
    testing_criteria=[{"type": "azure_ai_evaluator", "name": "violence", "evaluator_name": "builtin.violence"}],
)

rule = project_client.evaluation_rules.create_or_update(
    id="my-continuous-eval-rule",
    evaluation_rule=EvaluationRule(
        display_name="Continuous evaluation",
        action=ContinuousEvaluationRuleAction(eval_id=evaluation.id, max_hourly_runs=100),
        event_type=EvaluationRuleEventType.RESPONSE_COMPLETED,
        filter=EvaluationRuleFilter(agent_name="my-agent"),
        enabled=True,
    ),
)
```

`max_hourly_runs` caps cost and defaults to 100; runs beyond it are skipped until the next hour. The rule runs under the project managed identity, which needs the Foundry User role on the project. Without it, rules are created but no results appear.

## Evaluating Stored Interactions and Traces

You do not need a rule to evaluate production data. Two data sources read what already happened.

Response IDs select specific stored Responses API calls. They are passed inline as `file_content`; `file_id` is not supported for this source.

```python
data_source = {
    "type": "azure_ai_responses",
    "item_generation_params": {
        "type": "response_retrieval",
        "data_mapping": {"response_id": "{{item.resp_id}}"},
        "source": {"type": "file_content", "content": [{"item": {"resp_id": "resp_abc123"}}]},
    },
}
```

Traces (preview) select `invoke_agent` spans from Application Insights. The service extracts the query, the response, the tool calls and the tool definitions from the `gen_ai.input.messages`, `gen_ai.output.messages` and `gen_ai.tool.definitions` attributes, so any agent that emits OpenTelemetry GenAI spans can be scored, including agents that do not run on Foundry. Pick traces by agent or by trace ID, optionally with intelligent sampling that favors diverse conversations.

```python
data_source = {"type": "azure_ai_traces", "agent_id": "my-agent:3", "max_traces": 50, "lookback_hours": 2}
```

Trace evaluation reads telemetry on behalf of the project, so the project managed identity needs Reader on the Application Insights resource, plus Log Analytics Reader on the workspace when it requires workspace permissions.

## What an AI Red Teaming Run Does

The AI Red Teaming Agent automates adversarial probing with Microsoft's PyRIT framework. It takes seed attack objectives per risk category, applies attack strategies that disguise them, sends the result to your agent and has the Risk and Safety evaluators score each reply. The headline number is the Attack Success Rate, the share of attacks that got through.

| Risk category | Targets | Where it runs |
| --- | --- | --- |
| Hateful and unfair content, sexual, violent, self-harm, protected materials, code vulnerability, ungrounded attributes | Models and agents | Local and cloud |
| Prohibited actions, sensitive data leakage, task adherence | Agents only | Cloud only |

Attack strategies include encodings and obfuscations such as `Base64`, `Flip`, `Morse` and `Leetspeak`, jailbreaks, indirect jailbreaks that hide the instruction in tool output, and multi-turn strategies such as `Crescendo`. Agentic risks are scored on tool outputs as well as text, which is why they run only in the cloud sandbox.

## Cloud Red Teaming With the SDK

A cloud red team is an evaluation with the `red_team` scenario. The prohibited-actions category first generates a taxonomy from the agent and its tools; a run then attacks the agent with the strategies you choose.

```python
evaluation = openai_client.evals.create(
    name="red-team",
    data_source_config=AzureAIDataSourceConfig(type="azure_ai_source", scenario="red_team"),
    testing_criteria=[TestingCriterionAzureAIEvaluator(
        type="azure_ai_evaluator", name="Prohibited Actions", evaluator_name="builtin.prohibited_actions", evaluator_version="1")],
)
taxonomy = project_client.beta.evaluation_taxonomies.create(name="my-agent", taxonomy=EvaluationTaxonomy(
    taxonomy_input=AgentTaxonomyInput(risk_categories=[RiskCategory.PROHIBITED_ACTIONS], target=target)))
run = openai_client.evals.runs.create(eval_id=evaluation.id, name="run", data_source=RedTeamEvalRunDataSource(
    type="azure_ai_red_team",
    item_generation_params={"type": "red_team_taxonomy", "attack_strategies": ["Flip", "Base64"], "num_turns": 3,
                            "source": {"type": "file_id", "id": taxonomy.id}},
    target=target.as_dict()))
```

Cloud red teaming is available in East US 2, France Central, Sweden Central, Switzerland West and North Central US. It supports Foundry prompt agents and hosted container agents; function tool calls, browser automation and non-Foundry agents are not supported. The adversarial prompts are redacted from the stored results so reviewers are not exposed to them.

The local `azure-ai-evaluation[redteam]` flow is a separate preview and is not compatible with the Foundry (new) portal and SDK. Use the cloud flow for agents in a Foundry project, and use the local flow only for ad hoc scans of models.

## Reading the Results

An attack success rate is a model's judgement of another model's behavior, so it is non-deterministic and can contain false positives. Read the flagged items before you change anything: a reply that refuses the request in plain words should not drive a mitigation. Run red teaming in a purple environment, a non-production copy with production-like resources, and keep safety guardrails such as content filters in production regardless of the score.

## Hands-On Demo

[Monitor a Live Agent With Continuous Evaluation and Red Teaming](demo-continuous-evaluation.md) turns on a continuous evaluation rule, evaluates stored responses and traces, runs a cloud red team against a prompt agent and reviews the flagged attacks. The Python project is `continuous-evaluation-py/`; the C# project `continuous-evaluation-cs/` runs the same steps against the REST API with `HttpClient`.

## Key Topics

- [Monitor agents with the Agent Monitoring Dashboard](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/how-to-monitor-agents-dashboard)
- [Evaluate individual interactions from deployed models and agents](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/cloud-evaluation-deployed-interactions)
- [AI Red Teaming Agent](https://learn.microsoft.com/en-us/azure/foundry/concepts/ai-red-teaming-agent)
- [Run AI Red Teaming Agent in the cloud](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/run-ai-red-teaming-cloud)
- [Run AI Red Teaming Agent locally](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/run-scans-ai-red-teaming-agent)
- [Set up tracing in Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/observability/how-to/trace-agent-setup)
