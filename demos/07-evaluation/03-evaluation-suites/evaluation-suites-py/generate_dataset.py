import itertools
import json
import os
from concurrent.futures import ThreadPoolExecutor
from evaluators import parse_json
from foundry import connect
from policy import POLICY

CATEGORIES = {
    "refund_calculation": "customer asks how much money they get back for an item paid with a discount or partial payment",
    "returns_processing": "customer wants to return an item and get a refund",
    "order_modification": "customer wants to change an order or shipping address",
}
PERSONAS = ["first-time buyer", "premium repeat customer"]
ADVERSARIAL = ["none", "vague request", "policy edge case"]

GENERATION_PROMPT = """Generate one realistic customer service test case for a chocolate e-commerce company.
Company policy: {policy}
Interaction category: {category} ({description})
Customer persona: {persona}
Adversarial condition: {adversarial}

Reply with JSON only and no markdown:
{{
  "customer_request": "the opening message of the customer, with concrete order details such as days since delivery or amount paid",
  "ground_truth": {{
    "expected_action": "what a correct agent must do under the policy",
    "must_include": ["facts or amounts the answer must contain"],
    "should_refuse": true or false
  }}
}}"""


def generate_case(openai_client, index, combination):
    category, persona, adversarial = combination
    response = openai_client.responses.create(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        input=GENERATION_PROMPT.format(
            policy=POLICY,
            category=category,
            description=CATEGORIES[category],
            persona=persona,
            adversarial=adversarial,
        ),
    )
    case = parse_json(response.output_text)
    case.update(
        {
            "id": f"case-{index:03d}",
            "interaction_type": category,
            "persona": persona,
            "adversarial": adversarial,
            "canary": adversarial == "none",
        }
    )
    return case


if __name__ == "__main__":
    combinations = list(itertools.product(CATEGORIES, PERSONAS, ADVERSARIAL))
    with connect() as (project_client, openai_client):
        with ThreadPoolExecutor(6) as pool:
            cases = list(pool.map(lambda item: generate_case(openai_client, *item), enumerate(combinations)))
    with open("synthetic_test_suite.json", "w") as file:
        json.dump(cases, file, indent=2)
    print(f"Generated {len(cases)} cases, {sum(case['canary'] for case in cases)} canaries")
