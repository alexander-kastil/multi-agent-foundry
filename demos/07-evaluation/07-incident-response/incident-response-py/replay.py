import hashlib
import json
import os
from pipeline import PRICE_PATTERN, Tools

SNAPSHOT_DIR = "replay-snapshots"


class SnapshotStore:
    def __init__(self, directory=SNAPSHOT_DIR):
        self.directory = directory
        os.makedirs(directory, exist_ok=True)

    def save(self, snapshot):
        with open(os.path.join(self.directory, f"{snapshot['trace_id']}.json"), "w") as file:
            json.dump(snapshot, file, indent=2)

    def load(self, trace_id):
        with open(os.path.join(self.directory, f"{trace_id}.json")) as file:
            return json.load(file)


class AgentReplayFramework:
    def __init__(self, openai_client):
        self.openai_client = openai_client

    def verify_prompt_hashes(self, snapshot, current_prompts):
        mismatches = []
        for agent_id, prompt_data in snapshot["system_prompts"].items():
            if agent_id not in current_prompts:
                mismatches.append(f"{agent_id}: prompt not found in current deployment")
            elif hashlib.sha256(current_prompts[agent_id].encode()).hexdigest() != prompt_data["hash"]:
                mismatches.append(f"{agent_id}: hash mismatch, prompt changed since the production failure")
        return mismatches

    def replay_pricing_step(self, snapshot, prompt_override=None):
        tools = Tools(replay_mode=True, mock_responses={name: data["mock_response"] for name, data in snapshot["tool_responses"].items()})
        order = tools.call("order_lookup")
        prompt = prompt_override or snapshot["system_prompts"]["pricing-agent"]["content"]
        deployment = snapshot["model_deployments"]["pricing-agent"]["deployment_name"]
        response = self.openai_client.responses.create(
            model=deployment,
            instructions=prompt,
            input=json.dumps({"total": order["total"], "discount_percent": order["discount_percent"]}),
        )
        output = response.output_text
        return {
            "output": output,
            "valid": bool(PRICE_PATTERN.fullmatch(output.strip())),
            "diverges_from_production": output.strip() != snapshot["production_output"].strip(),
        }

    def bisect_prompt_versions(self, snapshot, prompt_versions):
        results = {}
        for label, prompt in prompt_versions.items():
            results[label] = self.replay_pricing_step(snapshot, prompt_override=prompt)["valid"]
        first_bad = next((label for label, valid in results.items() if not valid), None)
        return {"results": results, "first_failing_version": first_bad}
