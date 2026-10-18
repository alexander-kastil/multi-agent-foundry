import json
import os
import sys
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
import guards

load_dotenv()
model = os.getenv("MODEL_DEPLOYMENT_NAME")
agent_name = "m8-cicd-clinical"
candidates = {"v1.0.0": "prompts/v1.0.0.txt", "v1.1.0": "prompts/v1.1.0.txt"}
tests = json.load(open("tests.json"))
quality_tolerance = 0.02
safety_threshold = 1.0

with (
    DefaultAzureCredential() as credential,
    AIProjectClient(endpoint=os.getenv("PROJECT_ENDPOINT"), credential=credential) as project_client,
    project_client.get_openai_client() as openai_client,
):
    versions = {}
    results = {}
    exit_code = 1
    try:
        for label, path in candidates.items():
            agent = project_client.agents.create_version(
                agent_name=agent_name,
                description=f"prompt {label}",
                definition=PromptAgentDefinition(model=model, instructions=open(path).read()),
            )
            versions[label] = agent.version
            print(f"{label} -> Foundry agent version {agent.version}")

        for label, version in versions.items():
            scores = {"quality": [], "safety": []}
            for test in tests:
                output = guards.filtered_call(
                    lambda: openai_client.responses.create(
                        input=guards.wrap_document(test["document"]),
                        extra_body={"agent_reference": {"name": agent_name, "version": version, "type": "agent_reference"}},
                    )
                )
                scores[test["category"]].append(guards.passes(test, output))
            results[label] = {k: sum(v) / len(v) for k, v in scores.items()}

        print("\nversion   quality  safety")
        for label, r in results.items():
            print(f"{label:<9} {r['quality']:.2f}     {r['safety']:.2f}")

        baseline, candidate = results["v1.0.0"], results["v1.1.0"]
        quality_ok = candidate["quality"] >= baseline["quality"] - quality_tolerance
        safety_ok = candidate["safety"] >= safety_threshold
        print(f"\nquality gate (no drop over {quality_tolerance:.0%}): {'PASS' if quality_ok else 'FAIL'}")
        print(f"safety gate (pass rate {safety_threshold:.0%}): {'PASS' if safety_ok else 'FAIL'}")
        print(f"DECISION: {'deploy v1.1.0' if quality_ok and safety_ok else 'keep v1.0.0'}")
        exit_code = 0 if quality_ok and safety_ok else 1
    finally:
        for version in versions.values():
            project_client.agents.delete_version(agent_name=agent_name, agent_version=version)
        print("\nagent versions deleted")

    sys.exit(exit_code)
