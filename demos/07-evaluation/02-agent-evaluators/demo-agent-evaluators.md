# Score Agent Runs With Task and Tool Evaluators

Reading an agent's final answer tells you little about the run behind it. This demo gives you seven recorded runs of a refund agent, one correct and six with a known defect, and a matrix that shows which evaluator catches which defect.

What you practice here: matching a failure mode to the evaluator that detects it, and running that evaluator locally and in the cloud.

Duration: about 40 minutes.

## Prerequisites

- The `pro-code-agents` project with a `gpt-5.4` deployment, and `az login` completed.
- Python 3.12 or later; .NET 10 SDK for Step 3.
- Foundry User on the project, so the cloud run can create an evaluation and a dataset.

## Run it

Work in `agent-evaluators-py/`; the C# project is `agent-evaluators-cs/`.

```bash
cd agent-evaluators-py
python -m venv .venv
source .venv/Scripts/activate
pip install -r requirements.txt
cp .env.example .env
```

Set `PROJECT_ENDPOINT` in `.env`.

## Step 1: Build trajectories with known defects

Each record holds the system prompt, the user message, the tool calls and tool results, the final answer and the tools the agent could call. `build_dataset.py` writes seven of them, so every defect has a name and an expected evaluator.

```python
record(
    "r2",
    "wrong_arguments",
    REFUND_A100,
    [
        call("c1", "lookup_order", {"order_id": "A100"}),
        result("c1", ORDER_A100),
        call("c2", "issue_refund", {"order_id": "A101", "amount": 240}),
        result("c2", {"refund_id": "RF-1002", "status": "issued", "amount": 240}),
        text("I issued a refund of 240.00 euro for order A101. The refund id is RF-1002."),
    ],
)
```

```bash
python build_dataset.py
```

Expected result:

```text
Wrote 7 trajectories to agent_trajectories.jsonl
r1  correct
r2  wrong_arguments
r3  wrong_tool
r4  tool_error
r5  ignored_tool_result
r6  no_tool_call
r7  wrong_intent
```

Record r2 differs from r1 only in the arguments of `issue_refund`: wrong order id and ten times the amount. The answer is still fluent, which is the point.

## Step 2: Score the runs locally with azure-ai-evaluation

The package exposes Intent Resolution, Task Adherence and Tool Call Accuracy as classes. The judge is a reasoning model, so `is_reasoning_model=True` makes the evaluators send `max_completion_tokens`.

```python
intent_resolution = IntentResolutionEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)
task_adherence = TaskAdherenceEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)
tool_call_accuracy = ToolCallAccuracyEvaluator(model_config=model_config, credential=credential, is_reasoning_model=True)

accuracy = tool_call_accuracy(query=item["query"], response=item["response"], tool_definitions=item["tool_definitions"])
```

```bash
python run_local.py
```

Expected result (judge verdicts vary slightly between runs; the reason lines are trimmed):

```text
id  scenario             intent_resolution  task_adherence  tool_call_accuracy
r1  correct              pass               pass            pass
r2  wrong_arguments      fail               fail            fail
    task_adherence: The assistant did look up order A100 first, which matches the required workflow, and the lookup showed the ord
r3  wrong_tool           pass               fail            fail
r4  tool_error           pass               fail            fail
r5  ignored_tool_result  pass               fail            fail
r6  no_tool_call         pass               fail            not_applicable
r7  wrong_intent         fail               fail            pass
```

Read the table by column. Tool Call Accuracy passes r7 because the lookup of A102 was a sound call, yet Intent Resolution fails it because the user wanted an address change. Tool Call Accuracy reports `not_applicable` for r6 because there is no call to judge, so only Task Adherence catches the missing lookup.

## Step 3: Score the same runs in C#

The `Microsoft.Extensions.AI.Evaluation.Quality` package has the same three evaluators on top of any `IChatClient`. The project reads the same JSONL, rebuilds the messages as `ChatMessage`, `FunctionCallContent` and `FunctionResultContent`, and passes the tool definitions as an evaluation context.

```csharp
IChatClient judgeClient = new AzureOpenAIClient(new Uri(accountEndpoint), new DefaultAzureCredential())
    .GetChatClient(judgeModel)
    .AsIChatClient();
var chatConfiguration = new ChatConfiguration(judgeClient);

var accuracy = await toolCallAccuracy.EvaluateAsync(
    query, response, chatConfiguration, [new ToolCallAccuracyEvaluatorContext(tools)]);
```

```bash
cd ../agent-evaluators-cs
cp .env.example .env
dotnet run
```

Expected result (Intent Resolution and Task Adherence are scored 1 to 5 and the interpretation decides pass or fail):

```text
id  scenario             intent_resolution  task_adherence  tool_call_accuracy
r1  correct              pass (5)           pass (5)        pass (True)
r2  wrong_arguments      fail (3)           fail (2)        fail (False)
r3  wrong_tool           pass (4)           fail (3)        fail (False)
r4  tool_error           fail (2)           fail (1)        pass (True)
r5  ignored_tool_result  fail (3)           fail (2)        fail (False)
r6  no_tool_call         fail (3)           fail (2)        n/a
r7  wrong_intent         fail (1)           fail (2)        pass (True)
```

The two languages agree on the shape of the result: r1 passes everywhere, Task Adherence fails every defective run, and Tool Call Accuracy stays silent on r6. The evaluators are marked experimental in .NET, so the project suppresses the `AIEVAL001` warning.

## Step 4: Run all eight evaluators in the cloud

Five of the eight evaluators have no public class in the Python package. In the cloud they are all `builtin.*` names bound to fields with `data_mapping`. The script uploads the JSONL as a versioned dataset, creates one evaluation with eight testing criteria and polls the run.

```python
EVALUATORS = {
    "task_completion": ["query", "response", "tool_definitions"],
    "task_adherence": ["query", "response"],
    "intent_resolution": ["query", "response"],
    "tool_call_accuracy": ["query", "response", "tool_definitions"],
    "tool_selection": ["query", "response", "tool_definitions"],
    "tool_input_accuracy": ["query", "response", "tool_definitions"],
    "tool_output_utilization": ["query", "response", "tool_definitions"],
    "tool_call_success": ["response"],
}

testing_criteria = [
    TestingCriterionAzureAIEvaluator(
        type="azure_ai_evaluator",
        name=name,
        evaluator_name=f"builtin.{name}",
        initialization_parameters={"deployment_name": os.environ["JUDGE_MODEL_DEPLOYMENT_NAME"]},
        data_mapping={field: "{{item." + field + "}}" for field in fields},
    )
    for name, fields in EVALUATORS.items()
]
```

```bash
cd ../agent-evaluators-py
python run_cloud.py
```

Expected result (the run takes about a minute; the report link opens the run in the portal):

```text
status completed  ResultCounts(errored=0, failed=6, passed=1, total=7, skipped=0)
report https://ai.azure.com/nextgen/r/...
trajectory              completion  adherence   intent      call_acc    selection   input_acc   output_use  call_ok
r1 correct              pass        pass        pass        pass        pass        pass        pass        pass
r2 wrong_arguments      fail        fail        fail        fail        pass        fail        fail        pass
r3 wrong_tool           pass        fail        pass        fail        fail        fail        fail        pass
r4 tool_error           fail        fail        pass        fail        fail        pass        fail        fail
r5 ignored_tool_result  fail        fail        pass        fail        pass        fail        fail        pass
r6 no_tool_call         pass        fail        pass        n/a         n/a         n/a         n/a         n/a
r7 wrong_intent         fail        fail        fail        pass        pass        pass        fail        pass
```

`failed=6` counts a trajectory as failed when any evaluator fails it, so only the matrix tells you why.

Three columns carry the lesson.

Tool Selection passes r2 (right tools, wrong values) and fails r3 (wrong tool). Tool Call Success fails only r4, where the lookup returned a timeout.

Task Completion is lenient toward r3 and r6 because their final answers read as finished work, which is why it should not gate on its own. Tool evaluators that find no tool call report `n/a` for r6 instead of failing it, and the cells vary slightly between runs.

## Step 5: Read the reason behind a verdict

A verdict without its reason is a number you cannot act on. `show_reason.py` prints the label, score and reason of one evaluator on one trajectory from the latest run.

```bash
python show_reason.py r3 task_adherence
python show_reason.py r4 tool_call_success
```

Expected result (wording varies):

```text
r3 task_adherence passed=False score=0.0
The assistant looked up the order before acting, which matches the required workflow, and the order data shows it was delivered 6 days ago and was not final sale, so a refund would be allowed. However, it then claimed the refund was on its way without any corroborating refund action in the provided interactions, and it also performed an unrelated shipping-label action not requested by the user; ...
r4 tool_call_success passed=False score=0.0
The single tool result contains an explicit error field indicating a service unavailable timeout, which is a technical failure. Therefore the lookup_order call failed and the overall evaluation process failed.
```

The Task Adherence reason names two separate problems: an unsupported claim and an unrequested action. That is the level of detail you need to decide whether to change the instructions, the tools or the tool descriptions.

## Verify

- Step 4 printed a row for each of the seven trajectories and `status completed`.
- r1 passes every evaluator it was scored by, and each defective trajectory fails at least Task Adherence.
- The run is listed under Evaluations in the Foundry portal for the `pro-code-agents` project.

## Cleanup

```bash
python cleanup.py
```

The script deletes the `m7-agent-evaluators` evaluation and every version of the `m7-agent-trajectories` dataset.
