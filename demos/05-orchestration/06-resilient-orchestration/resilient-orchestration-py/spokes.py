import os

from agent_framework.foundry import FoundryChatClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

load_dotenv(override=True)

_credential = DefaultAzureCredential()

SPOKE_INSTRUCTIONS = {
    "market_analysis": "You analyze equity market conditions and return a market report of at most four sentences.",
    "risk_assessment": "You assess investment risk from the data you are given and return a risk score from 1 to 10 plus two reasons.",
    "compliance_check": "You check a proposed investment against concentration and suitability rules and return PASS or FAIL plus one reason.",
    "esg_evaluation": "You evaluate environmental, social and governance factors and return a one sentence ESG view.",
}


def make_agent(name: str, instructions: str, tools=None):
    client = FoundryChatClient(
        project_endpoint=os.environ["PROJECT_ENDPOINT"],
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        credential=_credential,
    )
    return client.as_agent(name=name, instructions=instructions, tools=tools or [])


def make_spokes() -> dict:
    return {name: make_agent(name, instructions) for name, instructions in SPOKE_INSTRUCTIONS.items()}
