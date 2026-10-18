import asyncio
import json
import sys

from hub import hub_and_spoke
from quorum import parallel_research, trip_circuit
from supervisor import supervise

QUERY = "Prepare a research report on adding equities exposure to the Contoso growth portfolio."
HEALTHY = {"market_analysis": 90, "risk_assessment": 90, "esg_evaluation": 90}
ONE_SLOW = {"market_analysis": 90, "risk_assessment": 90, "esg_evaluation": 0.01}
DEGRADED = {"market_analysis": 0.01, "risk_assessment": 90, "esg_evaluation": 0.01}
WEAK_REPORT = "We looked at the market. Equities seem fine."


async def run_hub() -> None:
    print(await hub_and_spoke(QUERY))


async def run_quorum() -> None:
    print("All spokes healthy")
    print(json.dumps(await parallel_research("Review MSFT for the growth portfolio.", HEALTHY)))
    print("ESG times out, two of three is enough")
    print(json.dumps(await parallel_research("Review MSFT for the growth portfolio.", ONE_SLOW)))
    print("Market analysis and ESG time out")
    print(json.dumps(await parallel_research("Review MSFT for the growth portfolio.", DEGRADED)))


async def run_circuit() -> None:
    await trip_circuit("compliance_check")


async def run_supervisor() -> None:
    outcome = await supervise(WEAK_REPORT)
    print(f"Approved: {outcome['approved']} after {outcome['attempts']} attempt(s)")


STAGES = {"hub": run_hub, "quorum": run_quorum, "circuit": run_circuit, "supervisor": run_supervisor}


async def main(selected: list[str]) -> None:
    for key in selected:
        print(f"\n{key}")
        await STAGES[key]()


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main(sys.argv[1:] or list(STAGES)))
