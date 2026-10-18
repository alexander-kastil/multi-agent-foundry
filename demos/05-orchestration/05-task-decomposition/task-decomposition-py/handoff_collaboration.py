from agent_framework.orchestrations import HandoffBuilder

from agents import make_agent


async def handoff_collaboration() -> None:
    sec_analyzer = make_agent(
        "sec_filing_analyzer",
        "You extract financial metrics from SEC filings. When foreign revenue exceeds 30 percent, hand off to international_risk_assessor. Do not summarize prior work, return findings in full structured form.",
        handoff=True,
    )
    international_risk = make_agent(
        "international_risk_assessor",
        "You assess currency exposure and geopolitical risk. The full conversation is available to you, build on it. Return JSON with currency_risk_score (0-10), hedging_recommendations and exposure_by_market. Out of scope: valuation, credit metrics.",
        handoff=True,
    )
    credit_analyst = make_agent(
        "credit_analyst",
        "You analyze debt structure and credit metrics. When you find foreign currency obligations, hand off to international_risk_assessor.",
        handoff=True,
    )
    workflow = (
        HandoffBuilder(
            name="financial_analysis_handoff",
            participants=[sec_analyzer, international_risk, credit_analyst],
        )
        .with_start_agent(sec_analyzer)
        .add_handoff(sec_analyzer, [international_risk])
        .add_handoff(credit_analyst, [international_risk])
        .with_autonomous_mode(turn_limits={"sec_filing_analyzer": 2, "international_risk_assessor": 2})
        .build()
    )
    result = await workflow.run(
        "Analyze ACME Corp's 10-K for investment risks. Foreign revenue is 60 percent across Brazil, India and Turkey.",
    )
    outputs = result.get_outputs()
    authors = []
    for output in outputs:
        for message in output.messages:
            if message.role == "assistant" and message.author_name not in authors:
                authors.append(message.author_name)
    print(f"Agents that spoke in order: {authors}")
    print(f"Final message: {outputs[-1].text}")
