import asyncio
import sys

from agents import make_agent
from agent_framework.declarative import WorkflowFactory

CLASSIFIER = 'Classify the customer message. Reply with raw JSON only: {"category": "billing" or "technical" or "other"}.'


async def main(path: str, messages: list[str]) -> None:
    factory = WorkflowFactory()
    factory.register_agent("m5-classifier", make_agent("m5-classifier", CLASSIFIER))
    factory.register_agent("m5-billing", make_agent("m5-billing", "You answer billing questions in two sentences."))
    factory.register_agent("m5-technical", make_agent("m5-technical", "You answer technical questions in two sentences."))
    workflow = factory.create_workflow_from_yaml_path(path)
    for message in messages:
        result = await workflow.run(message)
        print(f"{message} -> {result.get_outputs()}")


if __name__ == "__main__":
    asyncio.run(main("migration/exported.workflow.yaml", sys.argv[1:] or ["I was charged twice this month."]))
