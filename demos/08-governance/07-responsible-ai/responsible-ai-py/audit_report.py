import json
from collections import defaultdict
from statistics import mean, pstdev

entries = [json.loads(line) for line in open("out/audit.jsonl")]

print("Agent version timeline")
timeline = defaultdict(list)
for e in entries:
    timeline[(e["agent_id"], e["model_version"], e["prompt_version"])].append(e["timestamp"])
for (agent_id, model, prompt), stamps in timeline.items():
    print(f"  {agent_id} model={model} prompt={prompt} first={min(stamps)} last={max(stamps)} invocations={len(stamps)}")

print("\nQuality scores by team context")
by_team = defaultdict(list)
for e in entries:
    by_team[e["output"]["team_context"]].append(e["output"]["quality_score"])
for team, scores in sorted(by_team.items()):
    print(f"  {team:<11} avg={mean(scores):.1f} stdev={pstdev(scores):.1f} submissions={len(scores)}")

print("\nDecision trail for one correlation id")
target = entries[0]["correlation_id"]
for e in entries:
    if e["correlation_id"] == target:
        print(f"  {e['timestamp']} {e['action_type']} agent={e['agent_id']} input_hash={e['input_hash'][:16]} output={e['output']}")
