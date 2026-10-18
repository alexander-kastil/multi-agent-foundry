import asyncio
import sys

from dotenv import load_dotenv

from expense import build_workflow

load_dotenv(override=True)


async def main(decision: str, superstep: int | None) -> None:
    workflow, storage = build_workflow()
    if superstep is None:
        chosen = await storage.get_latest(workflow_name=workflow.name)
    else:
        candidates = await storage.list_checkpoints(workflow_name=workflow.name)
        chosen = [c for c in sorted(candidates, key=lambda c: c.timestamp) if c.iteration_count == superstep][0]
    print(f"resuming from checkpoint {chosen.checkpoint_id[:8]} (superstep {chosen.iteration_count})")
    pending = [
        event
        async for event in workflow.run(checkpoint_id=chosen.checkpoint_id, stream=True)
        if event.type == "request_info"
    ]
    print(f"re-emitted request: {pending[0].request_id}")
    async for event in workflow.run(responses={pending[0].request_id: decision}, stream=True):
        if event.type == "output":
            print(event.data)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main(sys.argv[1] if len(sys.argv) > 1 else "approve", int(sys.argv[2]) if len(sys.argv) > 2 else None))
