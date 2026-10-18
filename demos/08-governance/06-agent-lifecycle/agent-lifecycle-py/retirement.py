import json
import os
from datetime import date, datetime, timezone
import yaml
from foundry import project, create_agent, ask, delete_agent, pin_endpoint, served_version

TODAY = date(2026, 10, 18)

manifest = yaml.safe_load(open("manifests/security-analyzer-2.2.0.yaml"))["agent_version_manifest"]
replacement = yaml.safe_load(open("manifests/security-analyzer-2.3.0.yaml"))["agent_version_manifest"]
retirement = manifest["retirement"]
tenants = yaml.safe_load(open("tenants.yaml"))["tenants"]

old = create_agent(
    "m8-life-retiring",
    open(manifest["system_prompt"]["file"]).read(),
    metadata={"manifest_version": manifest["version"], "status": "deprecated"},
)
new = create_agent(
    "m8-life-retiring",
    open(replacement["system_prompt"]["file"]).read(),
    metadata={"manifest_version": replacement["version"], "status": "active"},
)
print(f"Registered {old.name} v{old.version} (2.2.0, deprecated) and v{new.version} (2.3.0, active)")

notice_days = (retirement["deletion_date"] - retirement["deprecated_date"]).days
print(f"\nGovernance: {notice_days} days between deprecation and deletion, minimum is 90: {'ok' if notice_days >= 90 else 'TOO SHORT'}")


def has_pin(tenant):
    pin = tenant["pin"]
    return bool(pin) and pin["agent_version"] == "2.2.0" and pin["expires"] > TODAY


def handle_request(tenant, prompt):
    days_left = (retirement["deletion_date"] - TODAY).days
    if has_pin(tenant):
        return 200, {
            "result": ask(old, prompt)[:80],
            "deprecation_warning": {
                "message": f"This agent version is deprecated and will be deleted on {retirement['deletion_date']}.",
                "migration_target": retirement["replacement_agent"],
                "days_until_deletion": days_left,
            },
        }
    return 410, {
        "error": "agent_deprecated",
        "message": f"Security Analyzer 2.2.0 was deprecated on {retirement['deprecated_date']} and will be deleted on {retirement['deletion_date']}.",
        "migration": {"replacement": retirement["replacement_agent"]},
        "deprecation_timeline": {"days_remaining": days_left},
    }


print("\nGraceful deprecation")
for tenant in tenants:
    if tenant["id"] in ("standard-northwind", "standard-contoso"):
        status, body = handle_request(tenant, "Is eval(user_input) a vulnerability?")
        print(f"  {tenant['id']}: HTTP {status} {json.dumps(body)[:200]}")

print("\nRetirement checklist")
pinned = [t["id"] for t in tenants if has_pin(t)]
latest_pin = max((t["pin"]["expires"] for t in tenants if has_pin(t)), default=None)
print(f"  tenant version pins on 2.2.0: {pinned or 'none'} (latest expiry {latest_pin})")
print(f"  replacement {replacement['version']} status: {replacement['status']}")

os.makedirs("out", exist_ok=True)
archive = {
    "agent": manifest["agent_id"],
    "version": manifest["version"],
    "archived_at": datetime.now(timezone.utc).isoformat(),
    "manifest": manifest,
}
json.dump(archive, open("out/archive-security-analyzer-2.2.0.json", "w"), indent=2, default=str)
print("  agent data archived to out/archive-security-analyzer-2.2.0.json")
print("  managed identity: disable now, delete after 90 days of audit retention")

blockers = [t for t in pinned if latest_pin and retirement["deletion_date"] < latest_pin]
print(f"\nDeletion date {retirement['deletion_date']} vs pins: {'BLOCKED by pins' if blockers else 'clear'}")

print("\nDeletion day (simulated): repoint the stable endpoint, then delete retired version 2.2.0 from Foundry")
pin_endpoint(old.name, new.version)
print(f"  endpoint selector now serves v{served_version(old.name)} ({replacement['version']})")
project.agents.delete_version(agent_name=old.name, agent_version=old.version)
remaining = [f"v{v.version}" for v in project.agents.list_versions(agent_name=old.name)]
print(f"  remaining versions: {remaining}")

delete_agent(new)
