import argparse
import json
import sys
from foundry import connect, delete_versions, deploy_versions
from suite_runner import run_suite


def compare(baseline, current, thresholds):
    checks = {
        "task_completion": (
            current["task_completion_rate"] - baseline["task_completion_rate"],
            -thresholds["task_completion_drop"],
        ),
        "goal_achievement": (
            current["avg_goal_achievement"] - baseline["avg_goal_achievement"],
            -thresholds["goal_achievement_drop"],
        ),
    }
    for name, rate in current["by_interaction_type"].items():
        checks[f"type:{name}"] = (rate - baseline["by_interaction_type"].get(name, rate), -thresholds["per_type_drop"])
    return {name: {"delta": delta, "limit": limit, "pass": delta >= limit} for name, (delta, limit) in checks.items()}


def report(baseline, current, comparison):
    lines = ["REGRESSION TEST RESULTS", "=" * 60]
    lines.append(f"Task completion  baseline {baseline['task_completion_rate']:.0%}  current {current['task_completion_rate']:.0%}")
    lines.append(f"Goal achievement baseline {baseline['avg_goal_achievement']:.1f}  current {current['avg_goal_achievement']:.1f}")
    for name, result in comparison.items():
        lines.append(f"{name:32s} delta {result['delta']:+.2f} limit {result['limit']:+.2f} {'PASS' if result['pass'] else 'FAIL'}")
    return "\n".join(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--save-baseline", action="store_true")
    parser.add_argument("--candidate", default="v2")
    arguments = parser.parse_args()

    with open("synthetic_test_suite.json") as file:
        suite = json.load(file)
    with open("historical_probes.json") as file:
        probes = json.load(file)
    with open("thresholds.json") as file:
        thresholds = json.load(file)

    with connect() as (project_client, openai_client):
        versions = deploy_versions(project_client)
        label = "v1" if arguments.save_baseline else arguments.candidate
        version = versions[label]
        canaries = [case for case in suite if case["canary"]]
        canary_result = run_suite(openai_client, version, canaries, f"{label}-canaries")
        print(f"Canaries ({label}): {canary_result['task_completion_rate']:.0%} completion")
        if canary_result["task_completion_rate"] < thresholds["canary_min_rate"] and not arguments.save_baseline:
            delete_versions(project_client, versions)
            print("BLOCK - canary failure")
            sys.exit(1)
        current = run_suite(openai_client, version, suite + probes, f"{label}-full")
        delete_versions(project_client, versions)

    if arguments.save_baseline:
        with open("baseline.json", "w") as file:
            json.dump(current, file, indent=2)
        print(f"Baseline saved: {current['task_completion_rate']:.0%} completion, {current['avg_goal_achievement']:.1f} avg score")
        sys.exit(0)

    with open("baseline.json") as file:
        baseline = json.load(file)
    comparison = compare(baseline, current, thresholds)
    print(report(baseline, current, comparison))
    if all(result["pass"] for result in comparison.values()):
        print("APPROVE DEPLOYMENT")
    else:
        print("BLOCK - quality regression detected")
        sys.exit(1)
