import asyncio
import time

from resilience import CIRCUITS, invoke_spoke

SPOKE_NAMES = ["market_analysis", "risk_assessment", "esg_evaluation"]


async def parallel_research(query: str, timeouts: dict[str, float], required: int = 2) -> dict:
    started = time.time()
    results = await asyncio.gather(
        *(invoke_spoke(name, query, timeouts[name]) for name in SPOKE_NAMES),
        return_exceptions=True,
    )
    successes = {r["spoke"]: r["result"] for r in results if isinstance(r, dict) and r["status"] == "success"}
    elapsed = round(time.time() - started, 1)
    if len(successes) < required:
        return {"status": "insufficient_quorum", "successful": len(successes), "required": required, "elapsed_sec": elapsed}
    return {
        "status": "success",
        "completed": sorted(successes),
        "missing": [name for name in SPOKE_NAMES if name not in successes],
        "elapsed_sec": elapsed,
    }


async def trip_circuit(spoke_name: str) -> None:
    for attempt in range(1, 5):
        outcome = await invoke_spoke(spoke_name, "Review MSFT for the growth portfolio.", timeout=0.01)
        print(f"Call {attempt}: {outcome['status']}, failures={CIRCUITS[spoke_name]['failures']}")
