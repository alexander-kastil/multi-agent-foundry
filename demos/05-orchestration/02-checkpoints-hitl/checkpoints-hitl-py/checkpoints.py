import asyncio

from agent_framework import FileCheckpointStorage

from expense import CHECKPOINT_DIR


async def main() -> None:
    storage = FileCheckpointStorage(CHECKPOINT_DIR, allowed_checkpoint_types=["expense:ApprovalRequest"])
    checkpoints = await storage.list_checkpoints(workflow_name="expense_approval")
    for checkpoint in sorted(checkpoints, key=lambda c: c.timestamp):
        pending = list(checkpoint.pending_request_info_events)
        print(f"{checkpoint.checkpoint_id[:8]}  superstep={checkpoint.iteration_count}  pending_requests={len(pending)}")


if __name__ == "__main__":
    asyncio.run(main())
