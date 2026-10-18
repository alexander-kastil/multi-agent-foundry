import json
import os
import sys
import yaml


def load_graph(path):
    return {a["name"]: a["depends_on"] for a in yaml.safe_load(open(path))["agents"]}


def deployment_waves(graph):
    waves = []
    done = set()
    while len(done) < len(graph):
        wave = sorted(n for n, deps in graph.items() if n not in done and set(deps) <= done)
        waves.append(wave)
        done.update(wave)
    return waves


def dependents_of(graph, agent):
    direct = {n for n, deps in graph.items() if agent in deps}
    result = set(direct)
    for d in direct:
        result |= dependents_of(graph, d)
    return result


def breaking_changes(baseline, current):
    found = []
    for agent, old in baseline.items():
        new = current.get(agent)
        if new is None:
            found.append((agent, "tool removed"))
            continue
        known = set(old["required_params"]) | set(old["optional_params"])
        for p in sorted(set(new["required_params"]) - set(old["required_params"])):
            reason = f"optional parameter '{p}' became required" if p in known else f"new required parameter '{p}'"
            found.append((agent, reason))
        for f in sorted(set(old["response_fields"]) - set(new["response_fields"])):
            found.append((agent, f"response field '{f}' removed"))
    return found


def render_workflow(graph, waves):
    lines = [
        "name: Deploy Multi-Agent System",
        "on:",
        "  workflow_dispatch:",
        "    inputs:",
        "      environment:",
        "        type: choice",
        "        options: [staging, production]",
        "        required: true",
        "jobs:",
    ]
    for name in [n for wave in waves for n in wave]:
        lines.append(f"  deploy-{name}:")
        if graph[name]:
            needs = ", ".join(f"deploy-{d}" for d in graph[name])
            lines.append(f"    needs: [{needs}]")
        lines += [
            "    runs-on: ubuntu-latest",
            "    steps:",
            f"      - name: Deploy {name} agent",
            "        run: |",
            "          az containerapp update \\",
            f"            --name {name}-agent \\",
            "            --resource-group fabrikam-${{ inputs.environment }} \\",
            f"            --image fabrikam.azurecr.io/{name}:${{{{ github.sha }}}}",
        ]
    return "\n".join(lines) + "\n"


graph = load_graph("agent-dependencies.yml")
waves = deployment_waves(graph)

print("Deployment waves (agents in one wave deploy in parallel)")
for i, wave in enumerate(waves, 1):
    print(f"  wave {i}: {', '.join(wave)}")

baseline = json.load(open("contracts/baseline.json"))
current = json.load(open("contracts/current.json"))
changes = breaking_changes(baseline, current)

print("\nContract validation against baseline")
for agent, reason in changes:
    print(f"  BREAKING {agent}: {reason}")
    print(f"           affected dependents: {', '.join(sorted(dependents_of(graph, agent)))}")

os.makedirs("out", exist_ok=True)
open("out/deploy-agents.yml", "w").write(render_workflow(graph, waves))
print("\nGenerated out/deploy-agents.yml from the dependency manifest")

print("\nGate result:", "FAIL, breaking changes block deployment" if changes else "PASS")
sys.exit(1 if changes else 0)
