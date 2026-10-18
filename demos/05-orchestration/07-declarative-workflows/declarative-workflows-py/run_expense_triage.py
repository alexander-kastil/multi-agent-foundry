import sys

sys.stdout.reconfigure(encoding="utf-8")

import asyncio

from agent_framework.declarative import WorkflowFactory

CASES = [
    {"amount": 420, "category": "travel"},
    {"amount": 5000, "category": "office"},
]


async def main() -> None:
    workflow = WorkflowFactory().create_workflow_from_yaml_path("workflows/expense-triage.yaml")
    print(f"loaded {workflow.name}")
    for case in CASES:
        result = await workflow.run(case)
        print(f"{case} -> {result.get_outputs()}")


if __name__ == "__main__":
    asyncio.run(main())
