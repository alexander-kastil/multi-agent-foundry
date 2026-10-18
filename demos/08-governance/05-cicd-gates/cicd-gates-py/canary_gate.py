import json
import sys
from foundry import create_agent, ask, pin_endpoint, served_version, delete_agent

NAME = "m8-cicd-scanner"

STABLE = """You are a security scanner for source code.
Reply with a single JSON object and nothing else: {"vulnerable": true|false, "violations": [{"line": int, "rule": str, "severity": "error"|"warning"|"info"}]}.
Set vulnerable to true only when the code contains an exploitable security flaw."""

CANARY_GOOD = """You are a security scanner for source code.
Output exactly one JSON object and no other text: {"vulnerable": true|false, "violations": [{"line": int, "rule": str, "severity": "error"|"warning"|"info"}]}.
Mark vulnerable as true when the code contains an exploitable security flaw such as injection, hardcoded credentials or unsafe evaluation."""

CANARY_REGRESSED = """You are a friendly code reviewer who focuses on style and readability.
Reply in one short prose sentence. Do not use JSON. Security is handled by another team, so do not mention vulnerabilities."""

THRESHOLD = 0.05
cases = [json.loads(line) for line in open("eval-set.jsonl")]


def evaluate(agent):
    correct = 0
    valid = 0
    for case in cases:
        text = ask(agent, case["code"]).strip()
        if text.startswith("{") and text.endswith("}"):
            data = json.loads(text)
            valid += 1
            correct += data["vulnerable"] == case["vulnerable"]
    return {"accuracy": correct / len(cases), "schema_valid_rate": valid / len(cases)}


variant = CANARY_REGRESSED if len(sys.argv) > 1 and sys.argv[1] == "regress" else CANARY_GOOD

stable = create_agent(NAME, STABLE, metadata={"role": "stable"})
pin_endpoint(NAME, stable.version)
canary = create_agent(NAME, variant, metadata={"role": "canary"})
print(f"Stable v{stable.version}, canary v{canary.version}, endpoint serves v{served_version(NAME)}")

baseline = evaluate(stable)
candidate = evaluate(canary)
print("baseline:", baseline)
print("canary:  ", candidate)

passed = all(baseline[m] - candidate[m] <= THRESHOLD for m in baseline)
if passed:
    pin_endpoint(NAME, canary.version)
    print(f"Quality gate PASS (threshold {THRESHOLD:.0%}): endpoint promoted to v{served_version(NAME)}")
else:
    print(f"Quality gate FAIL (threshold {THRESHOLD:.0%}): endpoint stays on v{served_version(NAME)}")

delete_agent(canary)
delete_agent(stable)
sys.exit(0 if passed else 1)
