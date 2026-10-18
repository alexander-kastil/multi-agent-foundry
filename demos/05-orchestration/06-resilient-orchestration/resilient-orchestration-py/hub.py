import json
from typing import Annotated

from agent_framework import tool
from pydantic import Field

from resilience import invoke_spoke
from spokes import make_agent

HUB_INSTRUCTIONS = (
    "You coordinate investment research for Contoso Capital by delegating to specialist agents. "
    "Always complete market analysis before you invoke risk assessment, and run compliance last. "
    "Each tool result carries a status; when a spoke did not succeed, say what is missing and continue with what you have. "
    "Finish with a short research report."
)


@tool(approval_mode="never_require")
async def invoke_market_analysis_agent(
    asset_classes: Annotated[list[str], Field(description="Asset classes to analyze, for example equities")],
) -> str:
    """Analyzes current market conditions for the given asset classes and returns a market report."""
    return json.dumps(await invoke_spoke("market_analysis", f"Analyze current conditions for: {asset_classes}"))


@tool(approval_mode="never_require")
async def invoke_risk_assessment_agent(
    market_data: Annotated[str, Field(description="Market analysis text the risk assessment is based on")],
) -> str:
    """Assesses investment risk based on a market analysis result."""
    return json.dumps(await invoke_spoke("risk_assessment", f"Assess risk based on: {market_data}"))


@tool(approval_mode="never_require")
async def invoke_compliance_agent(
    proposal: Annotated[str, Field(description="The investment proposal to check")],
) -> str:
    """Checks a proposed investment against concentration and suitability rules."""
    return json.dumps(await invoke_spoke("compliance_check", f"Check this proposal: {proposal}"))


async def hub_and_spoke(query: str) -> str:
    hub = make_agent(
        "research_orchestrator",
        HUB_INSTRUCTIONS,
        tools=[invoke_market_analysis_agent, invoke_risk_assessment_agent, invoke_compliance_agent],
    )
    result = await hub.run(query)
    return result.text
