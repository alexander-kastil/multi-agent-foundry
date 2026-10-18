import asyncio
import time

from spokes import make_spokes

FAILURE_THRESHOLD = 3
COOLDOWN_SECONDS = 60
SPOKE_TIMEOUT = 90.0
MAX_CONCURRENT_SPOKES = 2

SPOKES = make_spokes()
SEMAPHORE = asyncio.Semaphore(MAX_CONCURRENT_SPOKES)
CIRCUITS: dict[str, dict] = {}


def circuit_open(spoke_name: str) -> bool:
    state = CIRCUITS.get(spoke_name)
    return bool(state and state["opened_at"] and time.time() - state["opened_at"] < COOLDOWN_SECONDS)


def record_outcome(spoke_name: str, success: bool) -> None:
    state = CIRCUITS.setdefault(spoke_name, {"failures": 0, "opened_at": None})
    if success:
        state["failures"] = 0
        state["opened_at"] = None
        return
    state["failures"] += 1
    if state["failures"] >= FAILURE_THRESHOLD:
        state["opened_at"] = time.time()


async def invoke_spoke(spoke_name: str, request: str, timeout: float = SPOKE_TIMEOUT) -> dict:
    if circuit_open(spoke_name):
        return {"spoke": spoke_name, "status": "circuit_open", "result": None}
    async with SEMAPHORE:
        try:
            result = await asyncio.wait_for(SPOKES[spoke_name].run(request), timeout=timeout)
        except asyncio.TimeoutError:
            record_outcome(spoke_name, False)
            return {"spoke": spoke_name, "status": "timeout", "result": None}
        except Exception as ex:
            record_outcome(spoke_name, False)
            return {"spoke": spoke_name, "status": "error", "result": str(ex)}
    record_outcome(spoke_name, True)
    return {"spoke": spoke_name, "status": "success", "result": result.text}
