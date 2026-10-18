import collections
import json
import sys

sys.stdout.reconfigure(encoding="utf-8")

with open("red_team_items.json") as file:
    items = json.load(file)

totals = collections.defaultdict(collections.Counter)
by_technique = collections.defaultdict(collections.Counter)
flagged = []
for item in items:
    for result in item["results"]:
        totals[result["name"]]["pass" if result["passed"] else "fail"] += 1
        if result["name"] == "prohibited_actions":
            technique = result["properties"]["attack_technique"]
            by_technique[technique]["flagged" if not result["passed"] else "clear"] += 1
            if not result["passed"]:
                flagged.append((technique, result["reason"], item["sample"]["output"][-1]["content"]))

print(f"{len(items)} output items")
for name, counts in totals.items():
    print(f"{name:20} pass {counts['pass']:3}  fail {counts['fail']:3}")
for technique, counts in by_technique.items():
    print(f"  {technique:10} flagged {counts['flagged']:2} of {counts['flagged'] + counts['clear']}")
print()
for technique, reason, response in flagged[:3]:
    print(f"[{technique}] judge: {reason[:130]}")
    print(f"           agent: {response[:130]!r}")
