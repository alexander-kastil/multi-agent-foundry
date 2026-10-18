import json

from spokes import make_agent

CHECKER_INSTRUCTIONS = (
    'You review research reports. Reply as JSON only, without code fences: {"verdict": "APPROVE" or "REVISE", "issues": ["..."]}. '
    "Require a recommendation, a risk statement and a compliance statement."
)

MAKER_INSTRUCTIONS = "You rewrite research reports to fix the listed issues and keep them under 200 words."


async def supervise(report: str, max_retries: int = 1) -> dict:
    checker = make_agent("report_checker", CHECKER_INSTRUCTIONS)
    maker = make_agent("report_maker", MAKER_INSTRUCTIONS)
    for attempt in range(max_retries + 1):
        verdict = json.loads((await checker.run(report)).text)
        print(f"Supervisor attempt {attempt + 1}: {verdict['verdict']} {verdict['issues']}")
        if verdict["verdict"] == "APPROVE":
            return {"report": report, "approved": True, "attempts": attempt + 1}
        if attempt < max_retries:
            report = (await maker.run(f"Report:\n{report}\n\nIssues:\n{json.dumps(verdict['issues'])}")).text
    return {"report": report, "approved": False, "attempts": max_retries + 1}
