import copy
import difflib
import hashlib
from datetime import date, timedelta
import yaml
from foundry import project, create_agent, delete_agent

VALIDATION_DAYS = {1: 0, 2: 7, 3: 30}


def load_manifest(path):
    return yaml.safe_load(open(path))["agent_version_manifest"]


def prompt_text(manifest):
    return open(manifest["system_prompt"]["file"], "rb").read()


def classify(old, new):
    reasons = []
    if old["model"]["deployment_name"] != new["model"]["deployment_name"]:
        reasons.append("model deployment changed")
    ratio = difflib.SequenceMatcher(None, prompt_text(old).decode(), prompt_text(new).decode()).ratio()
    if 1 - ratio > 0.20:
        reasons.append(f"system prompt rewritten ({1 - ratio:.0%} of text modified)")
    if {t["name"] for t in new["tools"]} - {t["name"] for t in old["tools"]}:
        reasons.append("new tool integration")
    for metric, value in old["evaluation_baseline"].items():
        if new["evaluation_baseline"][metric] < value:
            reasons.append(f"evaluation baseline reduced ({metric})")
    return ("MANUAL approval", reasons) if reasons else ("AUTOMATED approval", ["tuning only"])


def eligible(tenant, approved_on, today):
    if tenant["pin"] and tenant["pin"]["expires"] > today:
        return f"held on {tenant['pin']['agent_version']} (pin until {tenant['pin']['expires']})"
    wait = VALIDATION_DAYS[tenant["rollout_tier"]]
    if today >= approved_on + timedelta(days=wait):
        return "deploy 2.3.0"
    return f"wait until {approved_on + timedelta(days=wait)}"


old = load_manifest("manifests/security-analyzer-2.2.0.yaml")
new = load_manifest("manifests/security-analyzer-2.3.0.yaml")

print("Manifest integrity (prompt file hash must match manifest)")
for m in (old, new):
    actual = hashlib.sha256(prompt_text(m)).hexdigest()
    print(f"  {m['version']}: {'match' if actual == m['system_prompt']['hash'] else 'DRIFT'}")

print("\nApproval gate")
verdict, reasons = classify(old, new)
print(f"  2.2.0 -> 2.3.0: {verdict} ({'; '.join(reasons)})")
tuned = copy.deepcopy(old)
tuned["model"]["temperature"] = 0.28
verdict, reasons = classify(old, tuned)
print(f"  2.2.0 -> temperature 0.28: {verdict} ({'; '.join(reasons)})")

print("\nCompliance fields in the 2.3.0 manifest")
for key in ("eu_ai_act_risk_classification", "material_change_assessment", "technical_file_reference"):
    print(f"  {key}: {new[key]}")

tenants = yaml.safe_load(open("tenants.yaml"))["tenants"]
approved_on = new["approval"]["approval_date"]
print(f"\nRollout of 2.3.0 approved on {approved_on}")
for offset in (0, 8, 31):
    today = approved_on + timedelta(days=offset)
    print(f"  day {offset} ({today})")
    for t in tenants:
        print(f"    {t['id']:<26} tier {t['rollout_tier']}: {eligible(t, approved_on, today)}")

print("\nPublish approved manifests as Foundry agent versions")
published = []
for m in (old, new):
    agent = create_agent(
        "m8-life-governed",
        prompt_text(m).decode(),
        metadata={
            "manifest_version": m["version"],
            "prompt_version": m["system_prompt"]["version"],
            "prompt_hash": m["system_prompt"]["hash"][:16],
            "approver": m["approval"]["approver"],
        },
    )
    published.append(agent)

for v in project.agents.list_versions(agent_name="m8-life-governed"):
    print(f"  {v.name} v{v.version} metadata={v.metadata}")

for agent in published:
    delete_agent(agent)
