import asyncio

from agents import make_agent
from agent_framework.declarative import WorkflowFactory

CLASSIFIER = 'Classify the message. Reply with raw JSON only: {"category": "billing" or "technical" or "other"}.'
MESSAGES = [
    "I was charged twice this month.",
    "The app crashes when I scan a receipt.",
    "Do you have a partner program?",
]


async def main() -> None:
    factory = WorkflowFactory()
    factory.register_agent("ClassifierAgent", make_agent("ClassifierAgent", CLASSIFIER))
    factory.register_agent("BillingAgent", make_agent("BillingAgent", "You answer billing questions in two sentences."))
    factory.register_agent("TechnicalAgent", make_agent("TechnicalAgent", "You answer technical questions in two sentences."))
    workflow = factory.create_workflow_from_yaml_path("workflows/support-router.yaml")
    for message in MESSAGES:
        result = await workflow.run({"message": message})
        print(f"{message} -> {result.get_outputs()}")


if __name__ == "__main__":
    asyncio.run(main())
