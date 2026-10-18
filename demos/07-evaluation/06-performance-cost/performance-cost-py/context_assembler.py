import re
import tiktoken

AGENT_BUDGETS = {"lookup": 2000, "reasoning": 4000, "policy": 8000}
IMPORTANT_PHRASES = ["i prefer", "please don't", "make sure to", "business account", "corporate"]
COMMITMENT_PHRASES = ["i'll", "we will", "expect"]


class ContextAssembler:
    def __init__(self):
        self.encoder = tiktoken.get_encoding("o200k_base")

    def count_tokens(self, text):
        return len(self.encoder.encode(text))

    def build_digest(self, conversation_history):
        digest = {
            "customer_identified": False,
            "customer_tier": "unknown",
            "order_number": None,
            "order_status": None,
            "return_eligible": None,
        }
        for turn in conversation_history:
            content = turn.get("content", "")
            lowered = content.lower()
            match = re.search(r"order #?(\d+)", content, re.IGNORECASE)
            if match:
                digest["order_number"] = match.group(1)
                digest["customer_identified"] = True
            if "gold member" in lowered:
                digest["customer_tier"] = "gold"
            if "return" in lowered and "not eligible" in lowered:
                digest["return_eligible"] = False
            if "delivered" in lowered and turn.get("role") == "assistant":
                digest["order_status"] = "delivered"
        return digest

    def filter_important_turns(self, conversation_history, importance_threshold=0.75):
        important = []
        for turn in conversation_history:
            content = turn.get("content", "").lower()
            score = 0.0
            if any(phrase in content for phrase in IMPORTANT_PHRASES):
                score = 0.85
            if turn.get("role") == "assistant" and any(phrase in content for phrase in COMMITMENT_PHRASES):
                score = max(score, 0.85)
            if score >= importance_threshold:
                important.append(turn)
        return important

    def compress_tool_results(self, tool_results):
        compressed = {}
        for tool_name, result in tool_results.items():
            if tool_name == "product_lookup":
                compressed[tool_name] = {
                    "name": result.get("product_name"),
                    "price": result.get("current_price"),
                    "stock": result.get("stock_level"),
                }
            elif tool_name == "order_lookup":
                compressed[tool_name] = {
                    "order_id": result.get("order_id"),
                    "status": result.get("fulfillment_status"),
                    "items": len(result.get("items", [])),
                }
        return compressed

    def assemble_context(self, conversation_history, system_prompt, tool_results, token_budget, window_turns=3):
        digest = self.build_digest(conversation_history)
        window = conversation_history[-window_turns * 2:]
        important = self.filter_important_turns(conversation_history[:-window_turns * 2])
        parts = {
            "system": [{"role": "system", "content": system_prompt}],
            "digest": [
                {"role": "system", "content": "Established facts: " + ", ".join(f"{k}={v}" for k, v in digest.items() if v)}
            ],
            "tools": [{"role": "system", "content": f"Tool results: {self.compress_tool_results(tool_results)}"}]
            if tool_results
            else [],
            "important": [dict(turn) for turn in important],
            "window": [dict(turn) for turn in window],
        }
        pruned = []

        def total():
            return sum(self.count_tokens(message["content"]) for group in parts.values() for message in group)

        if total() > token_budget:
            for message in parts["window"]:
                message["content"] = message["content"][:100] + "..."
            pruned.append("window summarized")
        if total() > token_budget:
            parts["important"] = []
            pruned.append("important turns dropped")
        if total() > token_budget:
            parts["tools"] = []
            pruned.append("tool results dropped")
        messages = [message for group in parts.values() for message in group]
        return messages, total(), pruned
