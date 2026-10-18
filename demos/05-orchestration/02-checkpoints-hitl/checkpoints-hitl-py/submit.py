import asyncio
import sys

from dotenv import load_dotenv

from expense import build_workflow

load_dotenv(override=True)

REPORT = "Team offsite dinner for 12 people at Gasthaus Zum Goldenen Hirschen, total 640 EUR."


async def main() -> None:
    workflow, storage = build_workflow()
    async for event in workflow.run(REPORT, stream=True):
        if event.type == "request_info":
            print(f"waiting for a human, request {event.request_id}")
            print(f"summary: {event.data.summary}")
    checkpoints = await storage.list_checkpoints(workflow_name=workflow.name)
    print(f"{len(checkpoints)} checkpoints on disk, the process now exits")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    asyncio.run(main())
