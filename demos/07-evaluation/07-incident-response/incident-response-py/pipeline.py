import hashlib
import json
import re
import time
import uuid
from foundry import PRICING_PROMPTS, ask_pricing_agent

ORDERS = {
    "A100": {"order_id": "A100", "total": 120.0, "discount_percent": 10},
    "A101": {"order_id": "A101", "total": 80.0, "discount_percent": 25},
    "A102": {"order_id": "A102", "total": 45.5, "discount_percent": 0},
    "A103": {"order_id": "A103", "total": 310.0, "discount_percent": 15},
}

PRICE_PATTERN = re.compile(r"\d+(\.\d{1,2})?")


class Tools:
    def __init__(self, replay_mode=False, mock_responses=None):
        self.replay_mode = replay_mode
        self.mock_responses = mock_responses or {}

    def call(self, name, **arguments):
        if self.replay_mode:
            return self.mock_responses[name]
        return ORDERS[arguments["order_id"]]


class TraceRecorder:
    def __init__(self):
        self.trace_id = uuid.uuid4().hex
        self.steps = []
        self.tool_responses = {}

    def step(self, name, started, status, **attributes):
        self.steps.append(
            {"name": name, "duration_ms": round((time.perf_counter() - started) * 1000), "status": status, **attributes}
        )


def process_payment(price_text):
    if PRICE_PATTERN.fullmatch(price_text.strip()):
        return {"status": "paid", "amount": float(price_text.strip())}
    return {"status": "rejected", "reason": "non_numeric_amount"}


def run_checkout(openai_client, order_id, version_label, version, recorder, tools=None):
    tools = tools or Tools()
    started = time.perf_counter()
    order = tools.call("order_lookup", order_id=order_id)
    recorder.tool_responses["order_lookup"] = order
    recorder.step("tool_order_lookup", started, "ok")

    started = time.perf_counter()
    request_text = json.dumps({"total": order["total"], "discount_percent": order["discount_percent"]})
    price_text = ask_pricing_agent(openai_client, version, request_text)
    recorder.step("agent_pricing", started, "ok", prompt_version=version_label, output=price_text)

    started = time.perf_counter()
    payment = process_payment(price_text)
    recorder.step("agent_payment", started, "ok" if payment["status"] == "paid" else "error", result=payment)
    return {"order_id": order_id, "price_text": price_text, "payment": payment}


def build_snapshot(recorder, order_id, version_label, model):
    prompt = PRICING_PROMPTS[version_label]
    return {
        "trace_id": recorder.trace_id,
        "input_data": {"order_id": order_id},
        "model_deployments": {"pricing-agent": {"deployment_name": model}},
        "system_prompts": {
            "pricing-agent": {"content": prompt, "hash": hashlib.sha256(prompt.encode()).hexdigest(), "version": version_label}
        },
        "tool_responses": {
            name: {"tool_name": name, "mock_response": {**response, "order_id": "MOCK-" + response["order_id"]}}
            for name, response in recorder.tool_responses.items()
        },
        "trace_timeline": recorder.steps,
        "production_output": next(step["output"] for step in recorder.steps if step["name"] == "agent_pricing"),
    }
