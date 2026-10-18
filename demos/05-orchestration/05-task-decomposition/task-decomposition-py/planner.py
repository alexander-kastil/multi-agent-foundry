import asyncio
import json

from agents import ask_json, make_agent
from registry import AGENT_REGISTRY

PLANNER_INSTRUCTIONS = "You are a research task decomposition planner. Reply with raw JSON only."

PLAN_PROMPT = """Research query: "{query}"

Available specialist agents:
{registry}

Return JSON: {{"complexity_assessment": "simple|moderate|complex", "subtasks": [{{"task_id": "T1", "description": "...", "assigned_agent": "registry name", "depends_on": []}}]}}

Rules: include only necessary subtasks, tasks without dependencies can run in parallel, simpler queries need fewer subtasks, a complex query ends with investment_thesis_generator."""

QUERIES = [
    "What is Microsoft's current stock price?",
    "Should Contoso Capital invest in Microsoft given current AI market trends and macroeconomic conditions?",
]


async def create_plan(query: str) -> dict:
    planner = make_agent("meta_agent_planner", PLANNER_INSTRUCTIONS)
    return await ask_json(planner, PLAN_PROMPT.format(query=query, registry=json.dumps(AGENT_REGISTRY, indent=2)))


async def execute_task(task: dict, context: dict, query: str) -> str:
    capabilities = AGENT_REGISTRY[task["assigned_agent"]]["capabilities"]
    specialist = make_agent(
        task["assigned_agent"],
        f"You are the {task['assigned_agent']} specialist with capabilities: {capabilities}. Answer in at most three sentences and invent plausible illustrative figures when you have no data.",
    )
    prompt = f"Original query: {query}\nTask: {task['description']}\nContext from prior tasks: {json.dumps(context) if context else 'none'}"
    return (await specialist.run(prompt)).text


async def execute_plan(plan: dict, query: str) -> dict:
    pending = {task["task_id"]: task for task in plan["subtasks"]}
    completed: dict[str, str] = {}
    wave = 0
    while pending:
        ready = [task for task in pending.values() if all(dep in completed for dep in task["depends_on"])]
        if not ready:
            raise ValueError(f"Plan has a dependency cycle or an unknown dependency among {list(pending)}")
        wave += 1
        print(f"Wave {wave}: {[task['task_id'] for task in ready]}")
        results = await asyncio.gather(
            *(execute_task(task, {dep: completed[dep] for dep in task["depends_on"]}, query) for task in ready)
        )
        for task, result in zip(ready, results):
            completed[task["task_id"]] = result
            del pending[task["task_id"]]
    return completed


async def dynamic_decomposition() -> None:
    for query in QUERIES:
        plan = await create_plan(query)
        print(f"\nQuery: {query}\nComplexity: {plan['complexity_assessment']}, subtasks: {len(plan['subtasks'])}")
        for task in plan["subtasks"]:
            print(f"  {task['task_id']} -> {task['assigned_agent']} depends_on={task['depends_on']}")
        completed = await execute_plan(plan, query)
        print(f"Final output: {completed[plan['subtasks'][-1]['task_id']]}")
