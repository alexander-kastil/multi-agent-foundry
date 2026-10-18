import os
import sys
import time
from foundry import PRICING_AGENT_NAME, connect, delete_pricing_versions, deploy_pricing_versions
from pipeline import ORDERS, TraceRecorder, build_snapshot, run_checkout
from postmortem import write_postmortem
from rca import HypothesisFramework, differential, first_failure_after
from remediation import CircuitBreaker, Gateway, GatewayRunbook, classify_severity
from replay import AgentReplayFramework, SnapshotStore

sys.stdout.reconfigure(encoding="utf-8", line_buffering=True)

with connect() as (project_client, openai_client):
    MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]
    versions = deploy_pricing_versions(project_client)
    deployed_prompts = {
        label: project_client.agents.get_version(agent_name=PRICING_AGENT_NAME, agent_version=version).definition.instructions
        for label, version in versions.items()
    }
    store = SnapshotStore()
    requests = []
    change_log = []
    order_ids = list(ORDERS)

    print("== Production traffic ==")
    for label in ("v1", "v2"):
        if label == "v2":
            change_log.append({"time": time.time(), "type": "prompt_deployment", "agent": "pricing-agent", "from": "v1", "to": "v2"})
        for index in range(6):
            recorder = TraceRecorder()
            order_id = order_ids[index % len(order_ids)]
            result = run_checkout(openai_client, order_id, label, versions[label], recorder)
            success = result["payment"]["status"] == "paid"
            timings = {step["name"]: step["duration_ms"] for step in recorder.steps}
            requests.append(
                {
                    "time": time.time(),
                    "trace_id": recorder.trace_id,
                    "success": success,
                    "pricing_ms": timings["agent_pricing"],
                    "tool_ms": timings["tool_order_lookup"],
                    "price_text": result["price_text"],
                }
            )
            if not success:
                store.save(build_snapshot(recorder, order_id, label, MODEL))
    failed = [r for r in requests if not r["success"]]
    error_rate = len(failed) / len(requests)
    print(f"{len(requests)} checkouts, {len(failed)} failed, error rate {error_rate:.0%}, severity {classify_severity(error_rate)}")

    print("\n== Root cause analysis ==")
    for name, stats in differential(requests).items():
        print(f"{name}: {stats}")
    framework = HypothesisFramework(change_log, requests)
    tested = framework.investigate()
    for result in tested:
        print(f"hypothesis '{result['hypothesis']}': {'CONFIRMED' if result['confirmed'] else 'eliminated'}")
    first_failure = first_failure_after(requests, change_log[0]["time"])
    print(f"first failure {first_failure - change_log[0]['time']:.1f}s after the prompt deployment")

    print("\n== Replay ==")
    replay = AgentReplayFramework(openai_client)
    snapshot = store.load(failed[0]["trace_id"])
    print(f"snapshot {snapshot['trace_id'][:8]} production output: {snapshot['production_output']!r}")
    print(f"hash check against current v1 prompt: {replay.verify_prompt_hashes(snapshot, {'pricing-agent': deployed_prompts['v1']})}")
    reproduced = replay.replay_pricing_step(snapshot)
    print(f"replay with captured prompt: output {reproduced['output']!r} valid {reproduced['valid']}")
    bisection = replay.bisect_prompt_versions(snapshot, deployed_prompts)
    print(f"bisection {bisection['results']} first failing version {bisection['first_failing_version']}")
    delete_pricing_versions(project_client, versions)

print("\n== Automated remediation ==")
primary = Gateway("primary", healthy=False)
backup = Gateway("backup")
config = {"payment_gateway": "primary"}
breaker = CircuitBreaker(window=20, probe_interval_seconds=0.5)
served_by = []
for _ in range(14):
    served_by.append(breaker.call(lambda: primary.charge(10), lambda: backup.charge(10))["gateway"])
print(f"breaker state {breaker.state}, last charges served by {served_by[-3:]}")
primary.healthy = True
time.sleep(0.6)
print(f"after probe: {breaker.call(lambda: primary.charge(10), lambda: backup.charge(10))['gateway']}, state {breaker.state}")
primary.healthy = False
runbook = GatewayRunbook(primary, backup, config)
print(f"runbook run 1: {runbook.run()}")
print(f"runbook run 2: {runbook.run()}")
print(f"gateway config: {config}")

incident = {
    "title": "Checkout failures after pricing agent prompt update",
    "summary": f"After pricing-agent v2 was deployed, {len(failed)} of {len(requests)} checkouts failed ({error_rate:.0%}) because the payment step rejected prices containing a currency symbol.",
    "timeline": [
        "Pricing agent prompt v2 deployed to 100% of traffic",
        f"First failed checkout {first_failure - change_log[0]['time']:.1f}s later",
        "Snapshots captured automatically for every failed trace",
        "Hypothesis framework confirmed prompt regression after eliminating model deployment change",
        "Replay with the v1 prompt produced valid output, bisection isolated v2",
    ],
    "root_cause": "Prompt v2 instructs the pricing agent to include the currency symbol and an explanation, which breaks the numeric contract with the payment step.",
    "contributing_factors": [
        "No output format validation between pricing and payment agents",
        "No canary deployment for prompt changes",
        "Regression suite had no price format probe",
    ],
    "went_well": ["Failed traces were captured for replay without manual flagging", "Bisection isolated the faulty prompt version in one replay run"],
    "went_badly": ["No automated alert fired on the checkout error rate until review"],
    "actions": [
        {"item": "Add output format validation to the pricing agent", "owner": "ai-engineering", "due": "2026-10-25"},
        {"item": "Canary every prompt deployment at 5% traffic", "owner": "platform", "due": "2026-11-01"},
        {"item": "Add price format probe to the regression suite", "owner": "ai-engineering", "due": "2026-10-25"},
    ],
}
write_postmortem("postmortem-output.md", incident)
print("\nPostmortem written to postmortem-output.md")
