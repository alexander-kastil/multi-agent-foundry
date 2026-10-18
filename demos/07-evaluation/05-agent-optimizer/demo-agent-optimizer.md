# Let the Optimizer Rewrite an Agent's Instructions

A ticket-triage agent with a one-line prompt answers every ticket with a friendly paragraph, and the routing system downstream wants a short label. Rewriting the prompt by hand is guesswork. In this demo you give the optimizer twenty labeled tickets and an evaluator, let it propose and score new instructions, promote the best candidate to a new agent version and verify the gain with your own evaluation.

What you practice here: turning a vague goal into a dataset and an evaluator that an optimizer can chase.

Duration: about 50 minutes, not counting the cloud time of the two optimization jobs (about 10 and 15 minutes), which you can overlap.

## Prerequisites

- The `pro-code-agents` project with `gpt-5.4` and `gpt-5-mini` deployed, and `az login` completed.
- Access to the agent optimizer preview; if job creation is rejected, ask for access before you continue.
- Python 3.12 or later, or the .NET 10 SDK for the C# project.

## Run it

Work in `agent-optimizer-py/`.

```bash
cd agent-optimizer-py
python -m venv .venv
source .venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
```

Set `PROJECT_ENDPOINT` in `.env`. `EVAL_MODEL_DEPLOYMENT_NAME` scores responses and `OPTIMIZATION_MODEL_DEPLOYMENT_NAME` generates candidates; the latter must be a `gpt-5` family or DeepSeek deployment.

Work in `agent-optimizer-cs/` for the C# version. It runs the same steps as subcommands of one console project, and it uses the prefix `m7c2-` so both projects can run side by side. The agent optimizer and code-based evaluators are preview features, so the project calls the documented REST API with `HttpClient`; `Foundry.cs` holds the one request helper, with a bearer token for `https://ai.azure.com/.default`.

```bash
cd agent-optimizer-cs
export PROJECT_ENDPOINT="https://<resource>.services.ai.azure.com/api/projects/<project>"
export MODEL_DEPLOYMENT_NAME="gpt-5.4"
export EVAL_MODEL_DEPLOYMENT_NAME="gpt-5-mini"
export OPTIMIZATION_MODEL_DEPLOYMENT_NAME="gpt-5.4"
dotnet run -- baseline
```

The subcommands are `baseline`, `optimize`, `show-candidates`, `promote`, `tool-tuning` and `cleanup`. In PowerShell, set the variables with `$env:PROJECT_ENDPOINT = "..."`. The C# project passes the tickets inline to the evaluation run, so it needs no dataset upload.

## Step 1: Score the baseline

`tickets.jsonl` holds twenty tickets with the category the routing system expects as ground truth: `billing`, `shipping`, `product` or `account`. The baseline agent knows its job only in outline.

```python
BASELINE_INSTRUCTIONS = "You triage customer support tickets for a chocolate shop."
```

`baseline.py` publishes it as version 1 and scores it with a cloud evaluation that sends each ticket to the agent and compares the answer with the label by `builtin.f1_score`, a token-overlap score between 0 and 1.

```python
"target": {"type": "azure_ai_agent", "name": AGENT_NAME, "version": str(version)},
"data_mapping": {"response": "{{sample.output_text}}", "ground_truth": "{{item.ground_truth}}"},
```

```bash
python baseline.py
```

Expected result:

```text
agent m7-ticket-triage-agent version 1
baseline version 1: mean f1 0.02 on 20 tickets
```

A friendly paragraph shares almost no tokens with a one-word label, so the score is near zero. This is the number the optimizer has to beat, and it is also the evaluation you reuse in Step 4.

C#: `dotnet run -- baseline` creates version 1 and runs the same evaluation. The target-completions run takes its twenty tickets inline as `file_content`.

```csharp
data_source = new
{
    type = "azure_ai_target_completions",
    source = new
    {
        type = "file_content",
        content = LoadTickets().Select(ticket => new { item = new { query = ticket.Query, ground_truth = ticket.GroundTruth } }),
    },
    input_messages = new
    {
        type = "template",
        template = new[]
        {
            new { type = "message", role = "user", content = new { type = "input_text", text = "{{item.query}}" } },
        },
    },
    target = new { type = "azure_ai_agent", name = AgentName, version },
},
```

Expected result:

```text
agent m7c2-ticket-triage-agent version 1
baseline version 1: mean f1 0.02 on 20 tickets
```

## Step 2: Run the optimization job

`optimize.py` sends the same twenty tickets inline as test cases and asks for up to three candidates that change the instructions only.

```python
job = AgentOptimizationJob(
    target_configuration=AgentOptimizationFoundryAgentTargetConfiguration(name=AGENT_NAME, version=version),
    optimization_model_configuration=AgentOptimizationModelConfiguration(model=os.environ["OPTIMIZATION_MODEL_DEPLOYMENT_NAME"]),
    optimization_configuration=AgentOptimizationConfiguration(
        evaluation_configuration=AgentOptimizationEvaluationConfiguration(
            training_set=AgentOptimizationTargetCompletionEvaluationSet(
                source=AgentOptimizationTargetCompletionInlineDataSource(test_cases=test_cases)
            ),
            evaluators=[AgentOptimizationEvaluator(name="builtin.f1_score")],
            evaluation_model=EvaluationModelConfiguration(model=os.environ["EVAL_MODEL_DEPLOYMENT_NAME"]),
            max_concurrent_agent_runs=4,
        ),
        candidate_search_configuration=AgentOptimizationCandidateSearchConfiguration(max_candidates=3),
        agent_optimization_space=AgentOptimizationSpace(target_attributes=[TargetAttribute.INSTRUCTIONS]),
    ),
)
poller = project_client.agents.begin_create_optimization_job(job=job, polling=False)
```

```bash
python optimize.py
```

The script prints the job ID, polls every 30 seconds and lists the candidates. Expected result after about 10 minutes (the repeated `status` lines are cut):

```text
optimization job opt_f40c7aa018e94e2d9e0f715d2ca565e6
  status JobStatus.IN_PROGRESS
  ...
  status JobStatus.SUCCEEDED
baseline 0.0118  best 0.1167  best candidate cand_opt_f40c7aa018e94e2d9e0f715d2ca565e6_0001
candidate_3  cand_opt_f40c7aa018e94e2d9e0f715d2ca565e6_0003  AgentOptimizationCandidateStatus.COMPLETED  score=0.0253035
candidate_2  cand_opt_f40c7aa018e94e2d9e0f715d2ca565e6_0002  AgentOptimizationCandidateStatus.COMPLETED  score=0.027077
candidate_1  cand_opt_f40c7aa018e94e2d9e0f715d2ca565e6_0001  AgentOptimizationCandidateStatus.COMPLETED  score=0.1166665
baseline  cand_opt_f40c7aa018e94e2d9e0f715d2ca565e6_0000  AgentOptimizationCandidateStatus.COMPLETED  score=0.011786
```

The job re-scores the baseline itself, so the optimizer's baseline (0.0118) and your Step 1 score (0.02) come from separate samples of the same agent.

C#: `dotnet run -- optimize` posts the job to `/agent_optimization_jobs` and polls the job until it ends. The body uses the wire names of the REST API.

```csharp
(await Project(HttpMethod.Post, "/agent_optimization_jobs", new
{
    display_name = displayName,
    target_configuration = new { name = agentName, version, type = "foundry_agent" },
    optimization_model_configuration = new { model = OptimizationModel },
    optimization_configuration = new
    {
        evaluation_configuration = new
        {
            training_set = new { source = new { test_cases = testCases, type = "inline" }, type = "target_completion" },
            evaluators = new[] { evaluator },
            evaluation_model = new { model = EvalModel },
            max_concurrent_agent_runs = maxConcurrentRuns,
        },
        candidate_search_configuration = new { max_candidates = maxCandidates },
        agent_optimization_space = new { target_attributes = new[] { attribute } },
        type = "agent_optimization",
    },
}))!;
```

Expected result (the repeated status lines are cut). In this run one of the three candidates failed and another was not scored, so the job ended with one scored candidate next to the baseline:

```text
optimization job opt_2ad2552596c64918bd36c1fbfd612dae
  status in_progress
  ...
  status succeeded
baseline 0.0134  best 0.2117  best candidate cand_opt_2ad2552596c64918bd36c1fbfd612dae_0001
candidate_2  cand_opt_2ad2552596c64918bd36c1fbfd612dae_0002  failed  score=
candidate_1  cand_opt_2ad2552596c64918bd36c1fbfd612dae_0001  completed  score=0.211667
baseline  cand_opt_2ad2552596c64918bd36c1fbfd612dae_0000  completed  score=0.013391
```

## Step 3: Read the candidates, not just the winner

A score alone hides the cost. `show_candidates.py` prints each candidate's improvement over the baseline together with the tokens and latency per answer, and what the job spent on the agent and on the optimization model.

```bash
python show_candidates.py
```

Expected result:

```text
job JobStatus.SUCCEEDED, termination AgentOptimizationTerminationReason.COMPLETED
candidate      score   delta  avg_tokens  latency_ms
baseline       0.012  +0.000         134        2811
candidate_1    0.117  +0.105         328        1520 *
candidate_2    0.027  +0.015         600        3026
candidate_3    0.025  +0.014         872        1602
agent         gpt-5.4       47376 tokens
optimization  gpt-5.4        8051 tokens
```

Candidate 1 gains 0.105, which is in the significant band of 0.10 to 0.20, and it also answers faster, but its answers use two and a half times the tokens of the baseline. Candidates 2 and 3 gain less than 0.03, which is noise. The same job is not deterministic: another run on the same agent reached 0.42 from a baseline of 0.016, so repeat a job before you rely on one outcome.

C#: `dotnet run -- show-candidates` prints the same table. Candidates that failed have no evaluation and are skipped.

Expected result:

```text
job succeeded, termination candidate_screening_stalled
candidate      score   delta  avg_tokens  latency_ms
baseline       0.013  +0.000         136        2452
candidate_1    0.212  +0.198         286        1600 *
agent         gpt-5.4       18218 tokens
optimization  gpt-5.4        6907 tokens
```

The termination reason differs from the Python run because only one candidate was scored; the gain of 0.198 is again in the strongest band, and the answers are faster but use about twice the tokens of the baseline.

## Step 4: Promote the winner and verify it yourself

`promote.py` prints the instructions the best candidate would apply, promotes it with `promote_optimization_candidate`, which creates a new prompt agent version, and scores that version with the evaluation from Step 1.

```python
promoted = project_client.agents.promote_optimization_candidate(job_id=job_id, candidate_id=best_id)
```

```bash
python promote.py
```

Expected result (the instructions are long; the start and the end are shown):

```text
instructions mutation:
You are a customer support ticket triage assistant for a chocolate shop.

Your job is NOT to draft a full customer-facing support reply. Instead, classify each incoming ticket into a concise triage output so it can be routed correctly.
...
Be consistent, terse, and optimized for label accuracy rather than conversational quality.
promoted candidate_1 to a new version of m7-ticket-triage-agent
version 2: mean f1 0.11 on 20 tickets
```

Your own evaluation of version 2 gives 0.11, which matches the optimizer's 0.117. The optimizer inferred from the examples that the agent should classify and be terse, but it invented categories such as "Damaged delivery" instead of your four labels, which is why the score stops far below 1.0. The fix is in the data: state the four labels in the baseline instructions, or add more examples per label, and run the job again.

C#: `dotnet run -- promote` posts to the `:promote` action of the best candidate and scores the new version with the evaluation from Step 1.

```csharp
JsonNode promoted = (await Foundry.Project(HttpMethod.Post, $"/agent_optimization_jobs/{jobId}/candidates/{bestId}:promote"))!;
```

Expected result (the instructions are long; the start and the end are shown):

```text
instructions mutation:
You are a customer support ticket triage assistant for a chocolate shop.

Your job is NOT to write a full customer-facing reply asking follow-up questions. Instead, classify each incoming ticket into the most appropriate support issue category using a short label only.

Input format:
...
- Do not ask clarifying questions.
promoted candidate_1 to a new version of m7c2-ticket-triage-agent
version 2: mean f1 0.19 on 20 tickets
```

## Step 5: Tune tool descriptions with a code-based evaluator

Instructions are one target; function tool descriptions are another.

The agent in `tool_tuning.py` has four function tools with deliberately useless names and descriptions: `op_a1` ("Does a lookup."), `op_a2` ("Does another lookup."), `op_b2` ("Does a search.") and `op_c3` ("Gets some data."). Twelve queries each expect one of them: `op_a1` for order status, `op_a2` for customer account data, `op_b2` for product search and `op_c3` for store hours. The optimizer may rewrite descriptions but not names, types or required fields.

Built-in tool evaluators skip a response that consists only of a function call, because a prompt agent's tools are not executed during optimization. The script registers a code-based custom evaluator, a preview feature that needs the `Evaluations=V1Preview` feature header, which reads the function call itself and scores the tool chosen and its main argument against `ground_truth`.

```python
GRADE_CODE = """def grade(sample: dict, item: dict) -> float:
    expected_tool, expected_value = item.get("ground_truth", "|").split("|", 1)
    calls = item.get("tool_calls") or (item.get("sample") or {}).get("tool_calls") or []
    names = []
    values = []
    for message in calls:
        for part in message.get("content", []):
            if part.get("type") == "function_call":
                names.append(part.get("name"))
                values.extend(str(value).lower() for value in (part.get("arguments") or {}).values())
    if expected_tool not in names:
        return 0.0
    return 0.5 + (0.5 if any(expected_value.lower() in value for value in values) else 0.0)
"""
```

```bash
python tool_tuning.py
```

The job takes about 15 minutes. Expected result (descriptions cut to 120 characters):

```text
agent m7-tool-agent version 5
evaluator m7_tool_choice version 3
optimization job opt_d7e26aa81e8541d8846acc5c38a90f19
status JobStatus.SUCCEEDED
baseline 0.3333333333333333  best 0.5
candidate_2  failed  score=None
candidate_1  completed  score=0.5
  op_a1: Look up order-specific information in an internal order management system. Use this for customer requests about a partic
  op_a2: Look up store-specific business information in an internal location or store-hours directory. Use this for questions abo
  op_b2: Search across broader information sources when the request is exploratory, unstructured, or not clearly tied to a known 
  op_c3: Retrieve structured data records from an internal data source when the task requires fetching concrete underlying data r
baseline  completed  score=0.3333333333333333
```

The score rises from 0.33 to 0.50, and the rewrite of `op_a1` is right: it names order lookups.

The rewrite of `op_a2` is wrong. It calls the account lookup a store-hours directory, because the optimizer never sees what a tool does; it only sees the queries, the agent's calls and the scores. A candidate that scores better can still describe a tool incorrectly, so read the new descriptions before you promote them, and do not promote this one.

Candidate 2 failed without a recorded reason, which can happen in any job; the job still succeeded because one candidate completed.

C#: `dotnet run -- tool-tuning` creates the tool agent, registers the evaluator with a `POST` to `/evaluators/{name}/versions` and runs the job with `tools` as the target attribute.

```csharp
var scorer = new
{
    name = Foundry.EvaluatorName,
    version = evaluatorVersion,
    initialization_parameters = new { deployment_name = Foundry.EvalModel, pass_threshold = 0.5 },
};
```

The grading function `gradeCode` is the same Python text as above. Expected result (descriptions cut to 120 characters; the score values differ from the Python run, because the job is not deterministic):

```text
agent m7c2-tool-agent version 1
evaluator m7c2_tool_choice version 1
optimization job opt_a29413f6b6344f6aa32aca2a8b806cf7
  status succeeded
baseline 0.4166666666666667  best 0.5
candidate_2  failed  score=
candidate_1  completed  score=0.5
  op_a1: Look up a specific order or shipment record by a direct identifier. Use this for customer support questions about an ind
  op_a2: Look up a specific store, branch, or location record by its exact name or locality. Use this for questions about a known
  op_b2: Run a broader search when the user does not provide a precise identifier or when you need discovery across possibly mult
  op_c3: Retrieve structured data associated with a known item, entity, or record after it has been identified. Use this when the
baseline  completed  score=0.4166666666666667
```

Read the descriptions before you promote them: this run again rewrote `op_a2`, the account lookup, as a store or location lookup.

## Step 6: Draft instructions with Prompt Optimizer in the portal

The agent optimizer needs data. When you only have a goal, the portal's Prompt Optimizer gives a first draft without any. This step has no script; the Foundry documentation describes it as follows.

1. Open Build, then Agents, and select the agent.
2. In the agent configuration panel, select the pencil with sparkle icon next to Instructions.
3. Optionally type a suggestion, for example "Always answer with one of billing, shipping, product or account", and select Optimize.
4. Review the changed paragraphs and the reasoning for each, add another suggestion if needed and optimize again.
5. Select Use prompt before you close the dialog; the result is not stored otherwise.

Then run Step 1's evaluation against the new version. Prompt Optimizer applies general practice, and only an evaluation on your data tells you whether the draft is better.

## Verify

- Step 1 printed a baseline f1 near zero and Step 4 printed a clearly higher f1 for the promoted version.
- `show_candidates.py` marks one candidate with `*` and shows a positive delta.
- `m7-ticket-triage-agent` has two versions: the baseline and the promoted candidate.

## Cleanup

`cleanup.py` deletes the optimization jobs, the evaluations and dataset versions of this demo, the `m7_tool_choice` evaluator versions and both agents. Each optimization job also leaves an evaluation named `agent_optimization_eval_opt_<job>` in the project; delete those in the portal under Evaluations.

```bash
python cleanup.py
```

For the C# project, run `dotnet run -- cleanup`. It deletes the optimization jobs of `m7c2-` agents, the evaluations of this demo including the `agent_optimization_eval_opt_<job>` evaluations each job leaves, the `m7c2_tool_choice` evaluator versions and both agents.

```bash
dotnet run -- cleanup
```
