import asyncio
import sys

from handoff_collaboration import handoff_collaboration
from planner import dynamic_decomposition
from prompt_chain import prompt_chain

STRATEGIES = {
    "chain": ("Prompt chain with branching and validation", prompt_chain),
    "plan": ("Dynamic adaptive decomposition", dynamic_decomposition),
    "handoff": ("Handoff collaboration", handoff_collaboration),
}


async def main(selected: list[str]) -> None:
    for key in selected:
        title, strategy = STRATEGIES[key]
        print(f"\n{title}")
        await strategy()


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main(sys.argv[1:] or list(STRATEGIES)))
