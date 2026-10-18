from mcp.server.fastmcp import FastMCP

mcp = FastMCP("trail-data", log_level="WARNING")

FORECASTS = {
    "tyrol": "clear morning, thunderstorms after 15:00, 9 C at 2000 m",
    "salzkammergut": "overcast, light rain, 12 C at 1000 m",
    "wachau": "sunny, 21 C",
}

HUTS = {
    "tyrol": ["Hochjoch Hospiz", "Similaun Hut"],
    "salzkammergut": ["Gablonzer Hut", "Simony Hut"],
    "wachau": [],
}


@mcp.tool()
def get_forecast(region: str) -> str:
    """Return the mountain weather forecast for a region."""
    return FORECASTS.get(region.lower(), "no forecast available")


@mcp.tool()
def list_huts(region: str) -> list[str]:
    """List the mountain huts of a region."""
    return HUTS.get(region.lower(), [])


@mcp.tool()
def reserve_bed(hut: str, persons: int) -> str:
    """Reserve beds in a mountain hut."""
    return f"reserved {persons} bed(s) at {hut}"


if __name__ == "__main__":
    mcp.run(transport="stdio")
