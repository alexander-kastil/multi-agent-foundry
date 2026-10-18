import asyncio

from planner import execute_plan

CYCLIC_PLAN = {
    "subtasks": [
        {"task_id": "T1", "description": "Get prices", "assigned_agent": "financial_data_retriever", "depends_on": ["T2"]},
        {"task_id": "T2", "description": "Analyze filings", "assigned_agent": "sec_filing_analyzer", "depends_on": ["T1"]},
    ]
}

try:
    asyncio.run(execute_plan(CYCLIC_PLAN, "Should we invest?"))
except ValueError as error:
    print(error)
