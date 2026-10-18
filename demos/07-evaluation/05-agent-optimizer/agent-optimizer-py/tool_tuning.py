import os
import time
from azure.ai.projects.models import (
    AgentOptimizationCandidateSearchConfiguration,
    AgentOptimizationConfiguration,
    AgentOptimizationEvaluationConfiguration,
    AgentOptimizationEvaluator,
    AgentOptimizationFoundryAgentTargetConfiguration,
    AgentOptimizationJob,
    AgentOptimizationModelConfiguration,
    AgentOptimizationSpace,
    AgentOptimizationTargetCompletionEvaluationSet,
    AgentOptimizationTargetCompletionInlineDataSource,
    AgentOptimizationTargetCompletionTestCase,
    EvaluationModelConfiguration,
    EvaluatorCategory,
    EvaluatorDefinitionType,
    FunctionTool,
    JobStatus,
    PromptAgentDefinition,
    TargetAttribute,
)
from common import EVALUATION_FEATURES, EVALUATOR_NAME, TOOL_AGENT_NAME, connect

def tool(name, description):
    return FunctionTool(
        name=name,
        description=description,
        parameters={
            "type": "object",
            "properties": {"value": {"type": "string", "description": "The value."}},
            "required": ["value"],
            "additionalProperties": False,
        },
        strict=True,
    )


TOOLS = [
    tool("op_a1", "Does a lookup."),
    tool("op_a2", "Does another lookup."),
    tool("op_b2", "Does a search."),
    tool("op_c3", "Gets some data."),
]

QUERIES = [
    ("Where is my order A100?", "op_a1|A100"),
    ("Has order B207 shipped yet?", "op_a1|B207"),
    ("Is order C310 out for delivery?", "op_a1|C310"),
    ("How many loyalty points does anna@example.com have?", "op_a2|anna@example.com"),
    ("Which address is stored for the account of tom@example.com?", "op_a2|tom@example.com"),
    ("Is the account lea@example.com still active?", "op_a2|lea@example.com"),
    ("Do you sell a vegan truffle box?", "op_b2|vegan truffle"),
    ("Show me dark chocolate bars under 5 euro.", "op_b2|dark chocolate"),
    ("Do you have hazelnut pralines?", "op_b2|hazelnut"),
    ("When does the Vienna shop open on Sunday?", "op_c3|Vienna"),
    ("What time do you close in Graz today?", "op_c3|Graz"),
    ("Opening hours in Salzburg tomorrow?", "op_c3|Salzburg"),
]

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

with connect() as (project_client, openai_client):
    agent = project_client.agents.create_version(
        agent_name=TOOL_AGENT_NAME,
        definition=PromptAgentDefinition(
            model=os.environ["MODEL_DEPLOYMENT_NAME"],
            instructions="You are a support agent for a chocolate shop. Call a tool when it answers the question.",
            tools=TOOLS,
        ),
    )
    print(f"agent {TOOL_AGENT_NAME} version {agent.version}")
    evaluator = project_client.evaluators.create_version(
        name=EVALUATOR_NAME,
        headers=EVALUATION_FEATURES,
        evaluator_version={
            "name": EVALUATOR_NAME,
            "categories": [EvaluatorCategory.QUALITY],
            "display_name": "Tool choice",
            "description": "Scores the tool name and the main argument of the first function call",
            "definition": {
                "type": EvaluatorDefinitionType.CODE,
                "code_text": GRADE_CODE,
                "init_parameters": {
                    "type": "object",
                    "properties": {"deployment_name": {"type": "string"}, "pass_threshold": {"type": "number"}},
                    "required": ["deployment_name", "pass_threshold"],
                },
                "metrics": {"result": {"type": "continuous", "desirable_direction": "increase", "min_value": 0.0, "max_value": 1.0}},
                "data_schema": {
                    "type": "object",
                    "required": ["ground_truth", "tool_calls"],
                    "properties": {"ground_truth": {"type": "string"}, "tool_calls": {"type": "array"}},
                },
            },
        },
    )
    print(f"evaluator {evaluator.name} version {evaluator.version}")
    job = AgentOptimizationJob(
        display_name="m7 tool descriptions",
        target_configuration=AgentOptimizationFoundryAgentTargetConfiguration(name=TOOL_AGENT_NAME, version=agent.version),
        optimization_model_configuration=AgentOptimizationModelConfiguration(model=os.environ["OPTIMIZATION_MODEL_DEPLOYMENT_NAME"]),
        optimization_configuration=AgentOptimizationConfiguration(
            evaluation_configuration=AgentOptimizationEvaluationConfiguration(
                training_set=AgentOptimizationTargetCompletionEvaluationSet(
                    source=AgentOptimizationTargetCompletionInlineDataSource(
                        test_cases=[AgentOptimizationTargetCompletionTestCase(query=q, ground_truth=g) for q, g in QUERIES]
                    )
                ),
                evaluators=[
                    AgentOptimizationEvaluator(
                        name=EVALUATOR_NAME,
                        version=evaluator.version,
                        initialization_parameters={"deployment_name": os.environ["EVAL_MODEL_DEPLOYMENT_NAME"], "pass_threshold": 0.5},
                    )
                ],
                evaluation_model=EvaluationModelConfiguration(model=os.environ["EVAL_MODEL_DEPLOYMENT_NAME"]),
            ),
            candidate_search_configuration=AgentOptimizationCandidateSearchConfiguration(max_candidates=2),
            agent_optimization_space=AgentOptimizationSpace(target_attributes=[TargetAttribute.TOOLS]),
        ),
    )
    poller = project_client.agents.begin_create_optimization_job(job=job, polling=False)
    job_id = poller.details.get("job_id")
    print(f"optimization job {job_id}")
    current = project_client.agents.get_optimization_job(job_id=job_id)
    while current.status not in {JobStatus.SUCCEEDED, JobStatus.FAILED, JobStatus.CANCELLED}:
        time.sleep(30)
        current = project_client.agents.get_optimization_job(job_id=job_id)
    print(f"status {current.status}")
    if current.status == JobStatus.FAILED:
        print(current.error.message if current.error else "no error")
    else:
        summary = current.result.candidate_summary
        print(f"baseline {summary.baseline_score}  best {summary.best_score}")
        for candidate in project_client.agents.list_optimization_candidates(job_id=job_id, expand=["mutations"]):
            score = candidate.evaluation.score if candidate.evaluation else None
            print(f"{candidate.name}  {candidate.status.value}  score={score}")
            for mutation in (candidate.as_dict().get("output") or {}).get("mutations", []):
                for tool in mutation["value"]:
                    function = tool["function"]
                    print(f"  {function['name']}: {function['description'][:120]}")
