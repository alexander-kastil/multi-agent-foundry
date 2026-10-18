import json

from agents import ask_json, make_agent
from registry import FILING_TEXT

BRANCHES = {
    "GROWTH_DRIVEN": "Produce a five year discounted cash flow summary with growth projections, terminal value assumption and intrinsic value estimate.",
    "MATURE_EARNINGS": "Produce a comparable multiples valuation using P/E and EV/EBITDA versus peers.",
    "ASSET_HEAVY": "Produce a book value based valuation.",
}


def validate_metrics(metrics: dict) -> dict:
    assert -1.0 <= metrics["operating_margin"] <= 1.0
    assert metrics["debt_to_equity"] >= 0
    return metrics


async def prompt_chain() -> dict:
    extractor = make_agent("metric_extractor", "Extract financial metrics from filing text. Reply with raw JSON only.")
    metrics = validate_metrics(
        await ask_json(
            extractor,
            f"Return JSON with revenue_millions, revenue_growth_3yr, operating_margin, debt_to_equity from: {FILING_TEXT}",
        )
    )
    print(f"Step 1 metrics: {json.dumps(metrics)}")

    classifier = make_agent("profile_classifier", "Classify company financial profiles. Reply with raw JSON only.")
    profile = await ask_json(
        classifier,
        f"Classify as GROWTH_DRIVEN, MATURE_EARNINGS or ASSET_HEAVY. Return JSON with classification, confidence, reasoning. Metrics: {json.dumps(metrics)}",
    )
    print(f"Step 2 profile: {profile['classification']}")

    valuer = make_agent("valuation_analyst", "You value companies. Reply with raw JSON only.")
    valuation = await ask_json(
        valuer,
        f"{BRANCHES[profile['classification']]} Reply as flat JSON with two string fields: method and valuation_summary (under 60 words). Metrics: {json.dumps(metrics)}",
    )
    print(f"Step 3 valuation method: {valuation['method']}")

    commentator = make_agent("analyst_commentator", "You write concise analyst commentary.")
    commentary = (await commentator.run(f"Write two sentences of commentary on: {json.dumps(valuation)}")).text
    print(f"Step 4 commentary: {commentary}")
    return valuation
