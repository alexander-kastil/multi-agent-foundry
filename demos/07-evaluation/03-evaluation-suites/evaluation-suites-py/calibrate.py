import json
import os
from concurrent.futures import ThreadPoolExecutor
from evaluators import judge, multi_judge_consensus
from foundry import connect

PASS_SCORE = 7


def cohen_kappa(labels_a, labels_b):
    total = len(labels_a)
    observed = sum(a == b for a, b in zip(labels_a, labels_b)) / total
    expected = sum(
        (labels_a.count(value) / total) * (labels_b.count(value) / total) for value in set(labels_a + labels_b)
    )
    return 1.0 if expected == 1 else (observed - expected) / (1 - expected)


if __name__ == "__main__":
    with open("calibration_set.json") as file:
        examples = json.load(file)
    model = os.environ["MODEL_DEPLOYMENT_NAME"]
    judge_models = os.environ["JUDGE_MODELS"].split(",")
    with connect() as (project_client, openai_client):
        with ThreadPoolExecutor(4) as pool:
            scores = list(
                pool.map(
                    lambda item: judge(
                        openai_client, model, item["customer_request"], item["response"], item["ground_truth"]
                    )["score"],
                    examples,
                )
            )
        human = [item["human_score"] >= PASS_SCORE for item in examples]
        automatic = [score >= PASS_SCORE for score in scores]
        kappa = cohen_kappa(human, automatic)
        difference = sum(abs(score - item["human_score"]) for score, item in zip(scores, examples)) / len(examples)
        print(f"Judge {model}: Cohen's kappa {kappa:.2f} on {len(examples)} labeled examples")
        print(f"Mean absolute score difference {difference:.1f}")
        print("Reliable for gating" if kappa > 0.7 else "Revise the rubric before gating")
        with ThreadPoolExecutor(4) as pool:
            consensus = list(
                pool.map(
                    lambda item: multi_judge_consensus(
                        openai_client, judge_models, item["customer_request"], item["response"], item["ground_truth"]
                    ),
                    examples,
                )
            )
    flagged = [index for index, result in enumerate(consensus) if result["needs_human_review"]]
    print(f"Consensus across {judge_models}: {len(flagged)} of {len(consensus)} examples need human review {flagged}")
    for index, result in enumerate(consensus):
        print(f"  example {index}: consensus {result['consensus_score']:.1f} spread {result['score_spread']} {result['individual_scores']}")
