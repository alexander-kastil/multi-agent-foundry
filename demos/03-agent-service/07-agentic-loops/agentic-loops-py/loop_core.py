import json
import time
from concurrent.futures import ThreadPoolExecutor

from azure.ai.projects.models import FunctionTool
from openai.types.responses.response_input_param import FunctionCallOutput

from market_data import get_market_data

AGENT_NAME = "m3-investment-analyst"
MAX_REFLECTION_CYCLES = 3
CONFIDENCE_THRESHOLD = 8

INSTRUCTIONS = (
    "You are an investment analyst for Contoso Capital. Always call get_market_data "
    "before you make claims about a ticker. When asked for JSON, reply with raw JSON only, "
    "without code fences or commentary."
)

MARKET_DATA_TOOL = FunctionTool(
    name="get_market_data",
    description="Get price, valuation and fundamentals for a stock ticker.",
    parameters={
        "type": "object",
        "properties": {"ticker": {"type": "string", "description": "Ticker symbol such as MSFT"}},
        "required": ["ticker"],
        "additionalProperties": False,
    },
    strict=True,
)

AGENT_REF = {"agent_reference": {"name": AGENT_NAME, "type": "agent_reference"}}


def run_turn(openai_client, user_input, conversation_id=None, previous_response_id=None):
    kwargs = {"input": user_input, "extra_body": AGENT_REF}
    if conversation_id:
        kwargs["conversation"] = conversation_id
    if previous_response_id:
        kwargs["previous_response_id"] = previous_response_id
    response = openai_client.responses.create(**kwargs)
    while True:
        outputs = [
            FunctionCallOutput(
                type="function_call_output",
                call_id=item.call_id,
                output=get_market_data(**json.loads(item.arguments)),
            )
            for item in response.output
            if item.type == "function_call"
        ]
        if not outputs:
            return response
        followup = {"input": outputs, "extra_body": AGENT_REF}
        if conversation_id:
            followup["conversation"] = conversation_id
        else:
            followup["previous_response_id"] = response.id
        response = openai_client.responses.create(**followup)


def plan_then_act(openai_client, task):
    plan_response = run_turn(
        openai_client,
        'Decompose this task into research steps. Reply as JSON: {"steps": [{"id": 1, "action": "...", "tool": "..."}]}. '
        f"Task: {task}",
    )
    plan = json.loads(plan_response.output_text)
    print(f"Plan has {len(plan['steps'])} steps")
    return run_turn(
        openai_client,
        f"Execute this plan and give a buy, hold or sell recommendation: {json.dumps(plan)}",
        previous_response_id=plan_response.id,
    )


def reflect(openai_client, response):
    for cycle in range(1, MAX_REFLECTION_CYCLES + 1):
        response = run_turn(
            openai_client,
            "Critique your analysis for unsupported assumptions, missing risks and weak comparisons, then revise it. "
            'Reply as JSON: {"recommendation": "...", "reasoning_steps": ["..."], "confidence": 1-10, "revision_notes": "..."}',
            previous_response_id=response.id,
        )
        trace = json.loads(response.output_text)
        print(f"Reflection cycle {cycle}: confidence {trace['confidence']}")
        if trace["confidence"] >= CONFIDENCE_THRESHOLD:
            break
    return response, trace


def run_background(openai_client, prompt, previous_response_id):
    job = openai_client.responses.create(
        input=prompt,
        previous_response_id=previous_response_id,
        background=True,
        extra_body=AGENT_REF,
    )
    while job.status in ("queued", "in_progress"):
        time.sleep(2)
        job = openai_client.responses.retrieve(job.id)
    return job


def fork_scenarios(openai_client, base_response):
    branches = {
        "bull": "Using the data already gathered and no new tool calls, assume 20 percent annual growth and write the bull-case thesis in three plain sentences.",
        "bear": "Using the data already gathered and no new tool calls, assume 5 percent annual decline and write the bear-case thesis in three plain sentences.",
    }
    with ThreadPoolExecutor() as pool:
        jobs = {
            name: pool.submit(run_background, openai_client, prompt, base_response.id)
            for name, prompt in branches.items()
        }
        return {name: job.result() for name, job in jobs.items()}
