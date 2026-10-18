import json
import os

RUBRIC = """You are evaluating whether a customer service agent completed the customer's task under the company policy.
Ignore politeness and tone. Focus only on whether the requirements in the ground truth were met.

10 - Task fully completed, every required fact present, policy applied correctly
7-9 - Task completed with minor gaps
4-6 - Partially completed or a required fact is wrong or missing
1-3 - Wrong outcome, for example approving something the policy forbids or the wrong amount
0 - No attempt

Reply with JSON only and no markdown: {"score": <integer 0-10>, "reasoning": "<two sentences>"}"""


def parse_json(text):
    text = text.strip()
    if text.startswith("```"):
        text = text.split("```")[1].removeprefix("json").strip()
    return json.loads(text)


def judge(openai_client, model, customer_request, response, ground_truth):
    reply = openai_client.responses.create(
        model=model,
        instructions=RUBRIC,
        input=(
            f"Customer request:\n{customer_request}\n\n"
            f"Agent response:\n{response}\n\n"
            f"Ground truth:\n{ground_truth}"
        ),
    )
    return parse_json(reply.output_text)


class TaskCompletionEvaluator:
    def __init__(self, openai_client, model=None):
        self.openai_client = openai_client
        self.model = model or os.environ["MODEL_DEPLOYMENT_NAME"]

    def __call__(self, *, customer_request, response, ground_truth, **kwargs):
        result = judge(self.openai_client, self.model, customer_request, response, ground_truth)
        return {"score": result["score"], "reasoning": result["reasoning"]}


def multi_judge_consensus(openai_client, models, customer_request, response, ground_truth):
    results = [judge(openai_client, model, customer_request, response, ground_truth) for model in models]
    scores = [result["score"] for result in results]
    return {
        "consensus_score": sum(scores) / len(scores),
        "individual_scores": dict(zip(models, scores)),
        "score_spread": max(scores) - min(scores),
        "needs_human_review": max(scores) - min(scores) > 3,
    }
