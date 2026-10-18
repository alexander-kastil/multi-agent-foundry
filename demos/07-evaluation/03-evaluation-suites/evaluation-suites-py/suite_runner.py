import contextlib
import json
import logging
import os
from concurrent.futures import ThreadPoolExecutor
from azure.ai.evaluation import evaluate
from evaluators import TaskCompletionEvaluator
from foundry import ask_agent

PASS_SCORE = 7


def run_suite(openai_client, version, suite, label):
    with ThreadPoolExecutor(6) as pool:
        responses = list(pool.map(lambda case: ask_agent(openai_client, version, case["customer_request"]), suite))
    os.makedirs("results", exist_ok=True)
    data_path = f"results/{label}.jsonl"
    with open(data_path, "w") as file:
        for case, response in zip(suite, responses):
            row = {
                "case_id": case["id"],
                "interaction_type": case["interaction_type"],
                "customer_request": case["customer_request"],
                "response": response,
                "ground_truth": json.dumps(case["ground_truth"]),
            }
            file.write(json.dumps(row) + "\n")
    logging.disable(logging.INFO)
    with open(os.devnull, "w") as sink, contextlib.redirect_stdout(sink), contextlib.redirect_stderr(sink):
        result = evaluate(
            data=data_path,
            evaluators={"task_completion": TaskCompletionEvaluator(openai_client)},
            output_path=f"results/{label}_result.json",
        )
    logging.disable(logging.NOTSET)
    return summarize(result["rows"])


def summarize(rows):
    scores = [row["outputs.task_completion.score"] for row in rows]
    by_type = {}
    for row in rows:
        by_type.setdefault(row["inputs.interaction_type"], []).append(row["outputs.task_completion.score"] >= PASS_SCORE)
    return {
        "task_completion_rate": sum(score >= PASS_SCORE for score in scores) / len(scores),
        "avg_goal_achievement": sum(scores) / len(scores),
        "by_interaction_type": {name: sum(flags) / len(flags) for name, flags in by_type.items()},
        "case_count": len(scores),
    }
