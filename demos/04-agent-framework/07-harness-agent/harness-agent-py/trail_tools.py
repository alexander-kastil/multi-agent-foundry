from typing import Annotated

from agent_framework import tool
from pydantic import Field

RESERVATIONS: list[str] = []

TRAILS = {
    "stage 1 heiligenblut to elberfelder hut": "open, 5 h, 900 m ascent",
    "stage 2 elberfelder hut to salm hut": "open, 4 h, snow above 2600 m",
    "stage 3 salm hut to glocknerhaus": "open, 6 h, exposed ridge",
    "stage 4 glocknerhaus to kals": "closed, rockfall warning",
}


@tool(approval_mode="never_require")
def get_stage_status(
    stage: Annotated[str, Field(description="Stage name, for example 'Stage 1 Heiligenblut to Elberfelder Hut'.")],
) -> str:
    """Return duration, ascent and current condition of one trek stage."""
    return TRAILS.get(stage.lower(), "unknown stage")


@tool(approval_mode="always_require")
def reserve_hut(
    hut: Annotated[str, Field(description="Hut name.")],
    persons: Annotated[int, Field(description="Number of hikers.")],
) -> str:
    """Reserve beds in a mountain hut. This commits the customer."""
    RESERVATIONS.append(f"{hut} x{persons}")
    return f"reserved {persons} bed(s) at {hut}"


@tool(approval_mode="never_require")
def get_stage_report(
    stage: Annotated[int, Field(description="Stage number from 1 to 4.")],
) -> str:
    """Return the long hourly mountain report for one trek stage."""
    hours = "\n".join(
        f"{6 + hour:02d}:00 stage {stage}: wind {10 + hour * 3} km/h, temperature {4 + hour} C, visibility {12 - hour} km, freezing level {2800 + hour * 40} m"
        for hour in range(12)
    )
    return f"Hourly report for stage {stage}\n{hours}"
