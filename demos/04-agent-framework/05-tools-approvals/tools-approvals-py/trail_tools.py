from typing import Annotated

from agent_framework import tool
from pydantic import Field

TRAILS = {
    "tauern high route": "open, snow above 2400 m",
    "wachau valley loop": "open, dry",
    "dachstein traverse": "closed, rockfall warning",
}


@tool(approval_mode="never_require")
def get_trail_status(
    trail: Annotated[str, Field(description="Trail name, for example 'Wachau valley loop'.")],
) -> str:
    """Return the current status of a hiking trail."""
    return TRAILS.get(trail.lower(), "unknown trail")


@tool(approval_mode="never_require")
def estimate_price(
    nights: Annotated[int, Field(description="Number of nights.")],
    persons: Annotated[int, Field(description="Number of hikers.")],
) -> str:
    """Estimate the hut price in euro for a stay: 38 euro per person and night."""
    return f"{nights * persons * 38} euro"


@tool(approval_mode="always_require")
def book_hut(
    hut: Annotated[str, Field(description="Hut name, for example 'Erzherzog Johann Hut'.")],
    nights: Annotated[int, Field(description="Number of nights.")],
    persons: Annotated[int, Field(description="Number of hikers.")],
) -> str:
    """Book a mountain hut. This charges the customer and cannot be undone."""
    return f"booked {hut} for {persons} person(s), {nights} night(s), confirmation HB-{sum(map(ord, hut)) % 9000 + 1000}"
