import json
from datetime import datetime, timezone

AGENT_PRIORITY = {"compliance": 100, "risk": 75, "market-analysis": 50, "reporting": 25}

JUDGE_INSTRUCTIONS = (
    "You evaluate whether two financial agent analyses are consistent. Reply with raw JSON only: "
    '{"status": "CONSISTENT" | "INCONSISTENT" | "COMPLEMENTARY", "reasoning": "...", "confidence": 0.0-1.0}'
)


def rule_based_conflict(output_a: dict, output_b: dict) -> dict | None:
    action_a = output_a.get("recommendation", {}).get("action")
    action_b = output_b.get("recommendation", {}).get("action")
    up = {"increase", "buy"}
    down = {"decrease", "sell"}
    if (action_a in up and action_b in down) or (action_a in down and action_b in up):
        return {"reason": "contradictory-actions", "actionA": action_a, "actionB": action_b}
    return None


def llm_consistency_check(openai_client, model: str, output_a: dict, output_b: dict) -> dict:
    response = openai_client.responses.create(
        model=model,
        instructions=JUDGE_INSTRUCTIONS,
        input=f"Agent A:\n{json.dumps(output_a)}\n\nAgent B:\n{json.dumps(output_b)}",
    )
    return json.loads(response.output_text)


def detect(openai_client, model: str, agent_a: str, output_a: dict, agent_b: str, output_b: dict) -> dict:
    rule = rule_based_conflict(output_a, output_b)
    if rule:
        return {"conflict": True, "type": "rule-based", "details": rule, "agentA": agent_a, "agentB": agent_b, "confidence": 1.0}
    judged = llm_consistency_check(openai_client, model, output_a, output_b)
    return {
        "conflict": judged["status"] == "INCONSISTENT",
        "type": "semantic",
        "details": judged["reasoning"],
        "agentA": agent_a,
        "agentB": agent_b,
        "confidence": judged["confidence"],
    }


def agent_type(agent_id: str) -> str:
    return agent_id.split("-v")[0]


def resolve(report: dict, output_a: dict, output_b: dict) -> dict:
    priority_a = AGENT_PRIORITY.get(agent_type(report["agentA"]), 0)
    priority_b = AGENT_PRIORITY.get(agent_type(report["agentB"]), 0)
    if priority_a == priority_b:
        return {"resolution": "escalated", "method": "orchestrator-synthesis", "requiresHumanReview": report["confidence"] < 0.5}
    winner_id, winner_output = (report["agentA"], output_a) if priority_a > priority_b else (report["agentB"], output_b)
    return {"resolution": "resolved", "method": "priority-based", "chosenAgent": winner_id, "output": winner_output}


class AuditTrail:
    def __init__(self):
        self.entries: list[dict] = []

    def log(self, tenant_id: str, report: dict, outcome: dict, outputs: dict) -> None:
        self.entries.append(
            {
                "timestamp": datetime.now(timezone.utc).isoformat(),
                "tenantId": tenant_id,
                "conflictType": report["type"],
                "agents": [report["agentA"], report["agentB"]],
                "confidence": report["confidence"],
                "resolution": outcome["resolution"],
                "method": outcome["method"],
                "chosenAgent": outcome.get("chosenAgent"),
                "snapshots": outputs,
            }
        )
